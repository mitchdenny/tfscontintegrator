using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using Microsoft.TeamFoundation.Client;
using ContinuousIntegrator.Properties;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Xml.Serialization;
using System.IO;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.Build.Proxy;

namespace ContinuousIntegrator
{
    public class ChangesetWatcher
    {
        private static TraceSwitch m_TraceSwitch = new TraceSwitch("General", string.Empty);

        private T GetService<T>()
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Getting service '{0}' from server '{1}'.",
                    typeof(T),
                    Settings.Default.TeamFoundationServerUrl
                    );
            }
#line default
            #endregion

            TeamFoundationServer server = TeamFoundationServerFactory.GetServer(Settings.Default.TeamFoundationServerUrl);
            T service = (T)server.GetService(typeof(T));

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Service '{0}' successfully retrieved from server '{1}'.",
                    typeof(T),
                    Settings.Default.TeamFoundationServerUrl
                    );
            }
#line default
            #endregion

            return service;
        }

        private int GetLastestChangesetId()
        {
            VersionControlServer versionControl = this.GetService<VersionControlServer>();
            int lastestChangeset = versionControl.GetLatestChangesetId();
            return lastestChangeset;
        }

        private int m_CurrentChangesetId = 0;

        private void Worker()
        {
            while (true)
            {
                try
                {
                    int changesetId = 0;
                    changesetId = this.WaitForChange();
                    changesetId = this.AllowCodeToSettle(changesetId);

                    int originalChangesetId = this.m_CurrentChangesetId;
                    this.m_CurrentChangesetId = changesetId;

                    this.RunBuilds(originalChangesetId, this.m_CurrentChangesetId);
                }
                catch (ThreadAbortException ex)
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                    {
                        Trace.TraceInformation("Thread abort was received. Assuming clean stop requested.");
                    }
#line default
                    #endregion

                    return;
                }
                catch (Exception ex)
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceError)
                    {
                        Trace.TraceError(
                            "An unexpected error occured whilst monitorinig for changesets:\n{0}",
                            ex
                            );
                    }
#line default
                    #endregion
                }
            }
        }

        private TriggerCollection FetchAllTriggers()
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Fetching trigger files from version control.");
            }
#line default
            #endregion

            VersionControlServer versionControl = this.GetService<VersionControlServer>();
            TeamProject[] projects = versionControl.GetAllTeamProjects(true);

            TriggerCollection allTriggers = new TriggerCollection();


            foreach (TeamProject project in projects)
            {
                TriggerCollection projectTriggers = this.FetchTriggersForTeamProject(project, versionControl);
                allTriggers.AddRange(projectTriggers);
            }

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(
                    "Fetched '{0}' from '{0}' team projects.",
                    allTriggers.Count,
                    projects.Length
                    );
            }
#line default
            #endregion

            return allTriggers;
        }

        private TriggerCollection FetchTriggersForTeamProject(TeamProject project, VersionControlServer versionControl)
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Fetching trigger file for team project '{0}'.",
                    project.Name
                    );
            }
#line default
            #endregion

            TriggerCollection triggers = new TriggerCollection();

            try
            {
                string tempFile = Path.GetTempFileName();
                string serverPath = string.Format("{0}/TeamBuildTypes/ContinuousIntegrator.xml", project.ServerItem);
                versionControl.DownloadFile(serverPath, tempFile);

                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation(
                        "Downloaded trigger file from '{0}' to '{1}'.",
                        serverPath,
                        tempFile
                        );
                }
#line default
                #endregion

                using (FileStream stream = new FileStream(tempFile, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(TriggerCollection));
                    triggers = (TriggerCollection)serializer.Deserialize(stream);
                }

                foreach (Trigger trigger in triggers)
                {
                    trigger.TeamProject = project.Name;
                }

                File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceWarning)
                {
                    Trace.TraceWarning(
                        "Could not download trigger file for the team project '{0}':\n{1}",
                        project.Name,
                        ex
                        );
                }
#line default
                #endregion
            }

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "The '{0}' team project has '{1}' continuous integration triggers defined.",
                    project.Name,
                    triggers.Count
                    );
            }
#line default
            #endregion

            return triggers;
        }

        private void RunBuilds(int originalChangesetId, int currentChangesetId)
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Running builds for server items referenced in changesets '{0}' to '{1}'.",
                    originalChangesetId + 1,
                    currentChangesetId
                    );
            }
#line default
            #endregion

            string[] serverItems = this.EnumerateServerItems(originalChangesetId, currentChangesetId);
            TriggerCollection triggers = this.FetchAllTriggers();
            BuildRequestCollection requests = this.GenerateRequests(serverItems, triggers);

            foreach (BuildRequest request in requests)
            {
                this.ProcessBuildRequest(request);
            }
        }

        private void WaitOrFail(string buildUri)
        {
            DateTime startTime = DateTime.Now;

            while (true)
            {
                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation(
                        "Getting build status for build URI '{0}'.",
                        buildUri
                        );
                }
#line default
                #endregion

                BuildStore store = this.GetService<BuildStore>();
                BuildData data = store.GetBuildDetails(buildUri);
                if ((data.BuildStatus == Microsoft.TeamFoundation.Build.Common.BuildConstants.BuildStatus.BuildSucceeded) ||
                    (data.BuildStatus == Microsoft.TeamFoundation.Build.Common.BuildConstants.BuildStatus.BuildStopped) ||
                    (data.BuildStatus == Microsoft.TeamFoundation.Build.Common.BuildConstants.BuildStatus.BuildFailed))
                {
                    return;
                }

                if (this.IsBuildTakingTooLong(startTime)) throw new Exception("Build is taking too long.");

                Thread.Sleep(5000);
            }
        }

        private bool IsBuildTakingTooLong(DateTime startTime)
        {
            TimeSpan buildDuration = DateTime.Now - startTime;
            if (buildDuration > Settings.Default.BuildTimeout) return true;
            return false;
        }

        private void ProcessBuildRequest(BuildRequest request)
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(
                    "Starting build for team project '{0}', build type '{1}' on build machine '{2}'.",
                    request.TeamProject,
                    request.BuildType,
                    request.BuildMachine
                    );
            }
#line default
            #endregion

            try
            {
                BuildParameters parameters = new BuildParameters();
                parameters.BuildDirectory = Settings.Default.BuildDirectory;
                parameters.BuildMachine = request.BuildMachine;
                parameters.BuildType = request.BuildType;
                parameters.RequestedBy = Thread.CurrentPrincipal.Identity.Name;
                parameters.TeamFoundationServer = Settings.Default.TeamFoundationServerUrl;
                parameters.TeamProject = request.TeamProject;

                BuildController controller = this.GetService<BuildController>();
                string buildUri = controller.StartBuild(parameters);

                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                {
                    Trace.TraceInformation(
                        "Build started for team project '{0}', build type '{1}' on build machine '{2}'. Build URI was '{3}'.",
                        request.TeamProject,
                        request.BuildType,
                        request.BuildMachine,
                        buildUri
                        );
                }
#line default
                #endregion

                this.WaitOrFail(buildUri);

                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                {
                    Trace.TraceInformation(
                        "Build completed successfully for build URI '{0}'.",
                        buildUri
                        );
                }
#line default
                #endregion
            }
            catch (Exception ex)
            {
                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceWarning)
                {
                    Trace.TraceWarning(
                        "Failed to start build for team project '{0}', build type '{1}' on build machine '{2}':\n{3}",
                        request.TeamProject,
                        request.BuildType,
                        request.BuildMachine,
                        ex
                        );
                }
#line default
                #endregion
            }
        }

        private BuildRequestCollection GenerateRequests(string[] serverItems, TriggerCollection triggers)
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Generating build requests.");
            }
#line default
            #endregion

            BuildRequestCollection requests = new BuildRequestCollection();

            foreach (string serverItem in serverItems)
            {
                foreach (Trigger trigger in triggers)
                {
                    if (trigger.Evaluate(serverItem))
                    {
                        BuildRequest request = new BuildRequest();
                        request.TeamProject = trigger.TeamProject;
                        request.BuildType = trigger.BuildType;
                        request.BuildMachine = trigger.BuildMachine;

                        if (!requests.Contains(request))
                        {
                            requests.Add(request);
                        }
                    }
                }
            }

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Generated '{0}' build requests.", requests.Count);
            }
#line default
            #endregion

            return requests;
        }

        private string[] EnumerateServerItems(int originalChangesetId, int currentChangesetId)
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Enumerating server items.");
            }
#line default
            #endregion

            List<string> serverItemList = new List<string>();

            VersionControlServer versionControl = this.GetService<VersionControlServer>();
            for (int changesetId = originalChangesetId + 1; changesetId < currentChangesetId + 1; changesetId++)
            {
                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation(
                        "Getting changeset '{0}'.",
                        changesetId
                        );
                }
#line default
                #endregion

                Changeset changeset = versionControl.GetChangeset(changesetId);

                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation(
                        "Changeset '{0}' had '{1}' changes.",
                        changesetId,
                        changeset.Changes.Length
                        );
                }
#line default
                #endregion
             
                foreach (Change change in changeset.Changes)
                {
                    if (!serverItemList.Contains(change.Item.ServerItem))
                    {
                        serverItemList.Add(change.Item.ServerItem);
                    }
                }
            }

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation(
                    "Enumerated '{0}' server items.",
                    serverItemList.Count
                    );
            }
#line default
            #endregion

            string[] serverItems = serverItemList.ToArray();
            return serverItems;
        }

        private bool MaxSleepTimeReached(DateTime startTime)
        {
            if ((DateTime.Now - startTime) > Settings.Default.MaximumSleepPeriod)
            {
                return true;
            }

            return false;
        }

        private int AllowCodeToSettle(int changesetId)
        {
            DateTime startTime = DateTime.Now;

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(
                    "Started waiting for code to settle at '{0}'.",
                    startTime
                    );
            }
#line default
            #endregion

            while (true)
            {
                if (this.MaxSleepTimeReached(startTime))
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceWarning)
                    {
                        Trace.TraceWarning(
                            "Code didn't settle after '{0}', settle period started at '{1}'.",
                            Settings.Default.MaximumSleepPeriod,
                            startTime
                            );
                    }
#line default
                    #endregion

                    return changesetId;
                }

                Thread.Sleep(Settings.Default.SleepPeriod);

                int latestChangesetId = this.GetLastestChangesetId();
                if (latestChangesetId == changesetId)
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                    {
                        Trace.TraceInformation(
                            "No change detected after changeset '{0}' in '{1}', sleeping since '{2}'.",
                            latestChangesetId,
                            Settings.Default.SleepPeriod,
                            startTime
                            );
                    }
#line default
                    #endregion

                    return changesetId;
                }
                else
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                    {
                        Trace.TraceInformation(
                            "Further changeset '{0}', was '{1}'.",
                            latestChangesetId,
                            changesetId
                            );
                    }
#line default
                    #endregion

                    changesetId = latestChangesetId;
                }
            }
        }

        private int WaitForChange()
        {
            while (true)
            {
                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation("Getting the latest changeset ID.");
                }
#line default
                #endregion

                int changesetId = this.GetLastestChangesetId();

                #region Tracing
#line hidden
                if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                {
                    Trace.TraceInformation("Latest changeset ID is '{0}'.", changesetId);
                }
#line default
                #endregion

                if (changesetId > this.m_CurrentChangesetId)
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
                    {
                        Trace.TraceInformation(
                            "The latest changeset ID has changed from '{0}' to '{1}'.",
                            this.m_CurrentChangesetId,
                            changesetId
                            );
                    }
#line default
                    #endregion

                    return changesetId;
                }
                else
                {
                    #region Tracing
#line hidden
                    if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
                    {
                        Trace.TraceInformation(
                            "The changeset has not changed, sleeping for '{0}'.",
                            Settings.Default.PollPeriod
                            );
                    }
#line default
                    #endregion

                    Thread.Sleep(Settings.Default.PollPeriod);
                }
            }
        }

        private Thread m_WorkerThread;

        public void Start()
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Starting the changeset watcher.");
            }
#line default
            #endregion

            if (this.m_WorkerThread != null)
            {
                throw new Exception("Worker thread was already started.");
            }

            this.m_CurrentChangesetId = this.GetLastestChangesetId();

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceInfo)
            {
                Trace.TraceInformation(
                    "Original changeset ID is '{0}'",
                    this.m_CurrentChangesetId
                    );
            }
#line default
            #endregion

            this.m_WorkerThread = new Thread(this.Worker);
            this.m_WorkerThread.IsBackground = true;
            this.m_WorkerThread.Start();

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Changeset watcher started successfully.");
            }
#line default
            #endregion
        }

        public void Stop()
        {
            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Stopping the changeset watcher.");
            }
#line default
            #endregion

            if (this.m_WorkerThread == null)
            {
                throw new Exception("Worker thread was not started.");
            }

            this.m_WorkerThread.Abort();
            this.m_WorkerThread = null;

            #region Tracing
#line hidden
            if (ChangesetWatcher.m_TraceSwitch.TraceVerbose)
            {
                Trace.TraceInformation("Changeset watcher stopped successfully.");
            }
#line default
            #endregion
        }
    }
}
