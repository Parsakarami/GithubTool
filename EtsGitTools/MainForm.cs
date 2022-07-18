using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EtsGitTools.Model.Repository;
using EtsGitTools.Model.Content;
using Model.Content;
using Model;
using System.Threading;
using System.IO;
using SonarCloudModel;
using SonarCloudModel.Measure;
using CsvHelper;
using System.Globalization;
using SonarCloudModel.Report;
using SonarCloudModel.Issue;

namespace EtsGitTools
{
    public partial class MainForm : Form
    {
        private List<Component> projectList = new List<Component>();
        List<ReportResult> metricResultList = new List<ReportResult>();
        List<Issue> issueList = new List<Issue>();
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private char separator => '@';
        private bool isInProgress;
        private bool IsInProgress
        {
            get { return isInProgress; }
            set
            {
                isInProgress = value;
                SpinnerPictureBox.Visible = value;
                SelectAllButton.Enabled = !value;
                ClearSelectionButton.Enabled = !value;
                ClearListButton.Enabled = !value;
                LoadButton.Enabled = !value;
                SaveButton.Enabled = !value;
                TabControl.Enabled = !value;
                RepoListBox.Enabled = !value;
                FindButton.Enabled = !value;
                SearchTextBox.Enabled = !value;
                CopyItemAddressButton.Enabled = !value;
            }
        }
        public MainForm()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            //Logout
            if (UserHelper.User != null && UserHelper.User.IsAuthenticated)
            {
                UserNameLabel.Text = "Login to use application.";
                UserNameLabel.ForeColor = SystemColors.Highlight;
                LoginButton.Text = "🔒 Login";
                UserHelper.User = null;
                TabControl.Enabled = false;
                FindButton.Enabled = false;
                SearchTextBox.Enabled = false;
                LoadButton.Enabled = false;
                SaveButton.Enabled = false;
                ClearSelectionButton.Enabled = false;
                SelectAllButton.Enabled = false;
                StopProcessButton.Enabled = false;
                ClearListButton.Enabled = false;
                CopyItemAddressButton.Enabled = false;
                return;
            }

            //Login
            InitializeConnectionForm frm = new InitializeConnectionForm();
            if (frm.ShowDialog() == DialogResult.OK)
            {
                if (UserHelper.User.IsAuthenticated)
                {
                    UserNameLabel.Text = $"{UserHelper.User.Username}";
                    UserNameLabel.ForeColor = Color.ForestGreen;
                    TabControl.Enabled = true;
                    FindButton.Enabled = true;
                    SearchTextBox.Enabled = true;
                    LoadButton.Enabled = true;
                    SaveButton.Enabled = true;
                    ClearSelectionButton.Enabled = true;
                    SelectAllButton.Enabled = true;
                    StopProcessButton.Enabled = true;
                    ClearListButton.Enabled = true;
                    CopyItemAddressButton.Enabled = true;
                    LoginButton.Text = "🔓 Logout";
                }
                else
                {
                    UserNameLabel.Text = $"Authentication Failed!";
                    UserNameLabel.ForeColor = Color.Maroon;
                    LoginButton.Text = "🔒 Login";
                }
            }
        }

        private void ClearListButton_Click(object sender, EventArgs e)
        {
            ClearResult();
        }

        private async void RemoveSelectedReposButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                if (RepoListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Repo selection is empty.", "Fork", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
                {
                    await DeleteRepo(item.Repo.owner.login, item.Repo.name);
                    //var proc = CommandLineHelper.ExecuteCommand($"gh repo delete {item.Repo.name} --confirm");
                }
                MessageBox.Show("Delete operation is complete.", "Fork", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Delete operation is faild.", "Fork", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void StopProcessButton_Click(object sender, EventArgs e)
        {
            IsInProgress = false;
        }

        private void ClearSelectionButton_Click(object sender, EventArgs e)
        {
            RepoListBox.ClearSelected();
        }

        private void SelectAllButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < RepoListBox.Items.Count; i++)
                RepoListBox.SetSelected(i, true);
        }

        private async void ForkSelectedRepoButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                if (RepoListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Repo selection is empty.", "Fork", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                await ForkRepoFromListBox();
                MessageBox.Show("Fork operation is complete.", "Fork", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fork operation is faild. Msg:" + ex.ToString(), "Fork", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void UpdateRecordCounts()
        {
            ResultsCountLabel.Text = $"Number of records: {RepoListBox.Items.Count.ToString()}";
        }
        private void FillRepoListBox(List<Repository> repoList, bool isNeedCleaning = true)
        {
            if (isNeedCleaning)
                RepoListBox.Items.Clear();

            foreach (var item in repoList)
                RepoListBox.Items.Add(new RepoListBoxItem { Repo = item, DisplayText = $"{item.name}{separator}{item.svn_url}" });

            RepoListBox.DisplayMember = "DisplayText";
            RepoListBox.ValueMember = "Repo";
            //{item.name}{separator}{item.svn_url}
            UpdateRecordCounts();
        }

        private async Task FetchRepos(string searchQuery, int per_page = 100, int page = 10)
        {
            try
            {
                var result = await SearchOnGithub(searchQuery, per_page);
                TotalItemFoundLabel.Text = $"Total Items:{result.total_count}";
                FillRepoListBox(result.items);
                for (int i = 2; i <= page; i++)
                {
                    bool isFetchFinished = result.total_count < ((i - 1) * per_page);
                    if (!IsInProgress || isFetchFinished)
                        break;

                    result = null;
                    result = await SearchOnGithub(searchQuery, per_page, i);
                    FillRepoListBox(result.items, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Operation Failed.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private async Task ForkRepoFromListBox()
        //{
        //    try
        //    {
        //        IsInProgress = true;
        //        bool isOrganizationSelected = !string.IsNullOrWhiteSpace(UserHelper.User.SelectedOrganization);
        //        foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
        //        {
        //            item.Repo.owner.login,item.Repo.name,
        //            if (isOrganizationSelected)
        //                CommandLineHelper.ExecuteCommand($"gh repo fork {item.Repo.svn_url} --org {UserHelper.User.SelectedOrganization}");
        //            else
        //                CommandLineHelper.ExecuteCommand($"gh repo fork {item.Repo.svn_url}");

        //            await Task.Delay(1000);
        //        }
        //    }
        //    finally
        //    {
        //        IsInProgress = false;
        //    }
        //}

        private async Task ForkRepoFromListBox()
        {
            try
            {
                IsInProgress = true;
                var currentAccountRepos = await GetAccountRepositories();
                bool isOrganizationSelected = !string.IsNullOrWhiteSpace(UserHelper.User.SelectedOrganization);
                foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
                {
                    if (currentAccountRepos.Any(a => a.name == item.Repo.name))
                        continue;

                    if (isOrganizationSelected)
                        await Fork(item.Repo.owner.login, item.Repo.name, UserHelper.User.SelectedOrganization);
                    else
                        await Fork(item.Repo.owner.login, item.Repo.name);
                    await Task.Delay(500);
                }
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async Task<SearchResponse> SearchOnGithub(string query, int per_page = 30, int page = 1)
        {
            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
            {
                var response = await client.GetAsync($"/search/repositories?q={query}&per_page={per_page}&page={page}&sort=updated");
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SearchResponse>(result);
                }
                else
                    throw new Exception("Cannot connect to the Github API.");
            }
        }

        private async Task<Repository> Fork(string owner, string repo, string targetOrganization = "")
        {
            try
            {
                using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                {
                    var content = new StringContent("{\"organization\":\"" + targetOrganization + "\"}");
                    var response = await client.PostAsync($"/repos/{owner}/{repo}/forks", content);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<Repository>(result);
                    }
                    else
                    {
                        //30 Minutes to pass the API call limit
                        await Task.Delay(TimeSpan.FromMinutes(31));
                        var newContent = new StringContent("{\"organization\":\"" + targetOrganization + "\"}");
                        var newResponse = await client.PostAsync($"/repos/{owner}/{repo}/forks", newContent);
                        if (newResponse.StatusCode == System.Net.HttpStatusCode.OK || newResponse.StatusCode == System.Net.HttpStatusCode.Accepted)
                        {
                            var newResult = await newResponse.Content.ReadAsStringAsync();
                            return JsonConvert.DeserializeObject<Repository>(newResult);
                        }
                        else
                            throw new AccessViolationException("You reached the Github API limit.");
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("Cannot connect to the Github API." + ex.ToString());
            }
        }

        private async Task<bool> DeleteRepo(string owner, string repo)
        {
            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
            {
                var response = await client.DeleteAsync($"/repos/{owner}/{repo}");
                if (response.StatusCode != System.Net.HttpStatusCode.NoContent || response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    return true;
                }
                else if (!response.IsSuccessStatusCode)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    response = await client.DeleteAsync($"/repos/{owner}/{repo}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception("Reach the secondray limit.");
                    }
                }
                else
                    throw new Exception("Cannot connect to the Github API.");
            }
        }

        private string BuildSearchQuery()
        {
            string logicalConnector = " ";
            string searchArea = "in:name,description,readme";

            if (!NameCheckBox.Checked)
                searchArea = searchArea.Replace("name,", "");

            if (!DescriptionCheckBox.Checked)
                searchArea = searchArea.Replace("description,", "");

            if (!ReadmeCheckBox.Checked)
                searchArea = searchArea.Replace(",readme", "");

            string searchQuery = string.Empty;
            //Content
            if (!string.IsNullOrWhiteSpace(QueryTextbox.Text))
                searchQuery += QueryTextbox.Text;

            //Language
            if (!string.IsNullOrWhiteSpace(LanguageTextbox.Text))
                searchQuery += logicalConnector + $"language:{LanguageTextbox.Text}";

            //Files
            if (!string.IsNullOrWhiteSpace(ContentTextbox.Text))
                searchQuery += logicalConnector + $"{ContentTextbox.Text} {searchArea}";

            //Number of stars
            bool isMinStarEntered = !string.IsNullOrWhiteSpace(MinStarTextbox.Text);
            bool isMaxStarEntered = !string.IsNullOrWhiteSpace(MaxStarTextbox.Text);
            if (isMinStarEntered && isMaxStarEntered)
                searchQuery += logicalConnector + $"stars:{MinStarTextbox.Text}..{MaxStarTextbox.Text}";
            else if (isMinStarEntered && !isMaxStarEntered)
                searchQuery += logicalConnector + $"stars:>={MinStarTextbox.Text}";
            else if (!isMinStarEntered && isMaxStarEntered)
                searchQuery += logicalConnector + $"stars:<={MaxStarTextbox.Text}";

            //Size
            bool isMinSizeEntered = !string.IsNullOrWhiteSpace(MinSizeTextbox.Text);
            bool isMaxSizeEntered = !string.IsNullOrWhiteSpace(MaxSizeTextbox.Text);
            if (isMinSizeEntered && isMaxSizeEntered)
                searchQuery += logicalConnector + $"size:{MinSizeTextbox.Text}..{MaxSizeTextbox.Text}";
            else if (isMinSizeEntered && !isMaxSizeEntered)
                searchQuery += logicalConnector + $"size:>={MinSizeTextbox.Text}";
            else if (!isMinSizeEntered && isMaxSizeEntered)
                searchQuery += logicalConnector + $"size:<={MaxSizeTextbox.Text}";

            //Number of forks
            bool isMinForksEntered = !string.IsNullOrWhiteSpace(NumberOfForksMinTextBox.Text);
            bool isMaxForksEntered = !string.IsNullOrWhiteSpace(NumberOfForksMaxTextBox.Text);
            if (isMinForksEntered && isMaxForksEntered)
                searchQuery += logicalConnector + $"forks:{NumberOfForksMinTextBox.Text}..{NumberOfForksMaxTextBox.Text}";
            else if (isMinForksEntered && !isMaxForksEntered)
                searchQuery += logicalConnector + $"forks:>={NumberOfForksMinTextBox.Text}";
            else if (!isMinForksEntered && isMaxForksEntered)
                searchQuery += logicalConnector + $"forks:<={NumberOfForksMaxTextBox.Text}";

            //Create datetime
            bool isMinCreatedDateEntered = CreationDateMinDateTimePicker.Value != CreationDateMinDateTimePicker.MinDate;
            bool isMaxCreatedDateEntered = CreationDateMaxDateTimePicker.Value != CreationDateMaxDateTimePicker.MinDate;
            if (isMinCreatedDateEntered && isMaxCreatedDateEntered)
                searchQuery += logicalConnector + $"created:{CreationDateMinDateTimePicker.Value:yyyy-MM-dd}..{CreationDateMaxDateTimePicker.Value:yyyy-MM-dd}";
            else if (isMinCreatedDateEntered && !isMaxCreatedDateEntered)
                searchQuery += logicalConnector + $"created:>={CreationDateMinDateTimePicker.Value:yyyy-MM-dd}";
            else if (!isMinCreatedDateEntered && isMaxCreatedDateEntered)
                searchQuery += logicalConnector + $"created:<={CreationDateMaxDateTimePicker.Value:yyyy-MM-dd}";

            //Update datetime
            bool isMinUpdateDateEntered = LastUpdateDateMinDateTimePicker.Value != LastUpdateDateMinDateTimePicker.MinDate;
            bool isMaxUpdateDateEntered = LastUpdateDateMaxDateTimePicker.Value != LastUpdateDateMaxDateTimePicker.MinDate;
            if (isMinUpdateDateEntered && isMaxUpdateDateEntered)
                searchQuery += logicalConnector + $"pushed:{LastUpdateDateMinDateTimePicker.Value:yyyy-MM-dd}..{LastUpdateDateMaxDateTimePicker.Value:yyyy-MM-dd}";
            else if (isMinUpdateDateEntered && !isMaxUpdateDateEntered)
                searchQuery += logicalConnector + $"pushed:>={LastUpdateDateMinDateTimePicker.Value:yyyy-MM-dd}";
            else if (!isMinUpdateDateEntered && isMaxUpdateDateEntered)
                searchQuery += logicalConnector + $"pushed:<={LastUpdateDateMaxDateTimePicker.Value:yyyy-MM-dd}";

            //Change the sort mode



            //Exclude
            return searchQuery;
        }

        private async void SearchButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                var query = BuildSearchQuery();
                //Fetch and fill the listbox
                await FetchRepos(query, 100, 10);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void ClearInputsButton_Click(object sender, EventArgs e)
        {
            QueryTextbox.Text =
            LanguageTextbox.Text =
            MinStarTextbox.Text =
            MaxStarTextbox.Text =
            MinSizeTextbox.Text =
            MaxSizeTextbox.Text =
            NumberOfForksMinTextBox.Text =
            NumberOfForksMaxTextBox.Text = string.Empty;

            CreationDateMinDateTimePicker.Value =
            CreationDateMaxDateTimePicker.Value =
            LastUpdateDateMinDateTimePicker.Value =
            LastUpdateDateMaxDateTimePicker.Value =
            CreationDateMinDateTimePicker.MinDate;
        }

        private void KeyPressHandler(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) &&
                !char.IsDigit(e.KeyChar) &&
                e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private async void GetListOfRepositoryButton_Click(object sender, EventArgs e)
        {
            try
            {
                //Fetch organization repos
                //var response = await client.GetAsync($"/orgs/EtsTest-iOSApps/repos?per_page=100&page={pageCount}");
                ClearResult();
                IsInProgress = true;
                using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                {
                    var pageCount = 1;
                    bool remainingItem = true;
                    while (remainingItem)
                    {
                        if (!IsInProgress)
                            break;

                        var response = await client.GetAsync($"/user/repos?per_page=100&page={pageCount}");
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            var repoList = JsonConvert.DeserializeObject<List<Repository>>(result);
                            FillRepoListBox(repoList, false);
                            remainingItem = (repoList.Count > 0);
                            pageCount++;
                            await Task.Delay(50);
                        }
                        else
                            remainingItem = false;
                    }

                }
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async void PushExclusionFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                if (RepoListBox.SelectedItems.Count == 0)
                {
                    MessageBox.Show("You must select a repository.", "Push", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
                {
                    if (item.Framework != TargetFramework.ReactNative)
                        if (string.IsNullOrWhiteSpace(item.SourceCodePath))
                        {
                            MessageBox.Show("You must find the source code path first!", "Push", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }

                    //Create .sonarcloud.properties file content
                    var fileContent = GenereteSonarCloudConfigFileContent(item.SourceCodePath, item.Framework);
                    var owner = item.Repo.owner.login;
                    var repoName = item.Repo.name;
                    var fileName = ".sonarcloud.properties";
                    using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                    {
                        //If find the config file, remove it and then create it
                        //If there is no config file, just create a new one

                        ContentResponse fileInfo = null;
                        var getContentResponse = await client.GetAsync($"/repos/{owner}/{repoName}/contents/{fileName}");
                        if (getContentResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var getContentResult = await getContentResponse.Content.ReadAsStringAsync();
                            fileInfo = JsonConvert.DeserializeObject<ContentResponse>(getContentResult);
                        }

                        //Add or Update file
                        var payload = new ContentPayload()
                        {
                            message = "Push SonarCloud config file.",
                            sha = fileInfo != null ? fileInfo.sha : "",
                            content = Convert.ToBase64String(fileContent),
                        };
                        var payloadJson = JsonConvert.SerializeObject(payload);
                        var content = new StringContent(payloadJson);

                        var response = await client.PutAsync($"/repos/{owner}/{repoName}/contents/{fileName}", content);
                        //if (response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
                        //    MessageBox.Show("Configuration file was pushed successfully.", "Push", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    await Task.Delay(50);
                }
                MessageBox.Show("Configuration file(s) was pushed successfully.", "Push", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async void DeleteSonarCloudExclusionFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
                {
                    if (string.IsNullOrWhiteSpace(item.SourceCodePath))
                    {
                        MessageBox.Show("You must find the source code path first!", "Push", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }

                    var owner = item.Repo.owner.login;
                    var repoName = item.Repo.name;
                    var fileName = ".sonarcloud.properties";
                    using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                    {
                        //If find the config file, remove it 
                        ContentResponse fileInfo = null;
                        var getContentResponse = await client.GetAsync($"/repos/{owner}/{repoName}/contents/{fileName}");
                        if (getContentResponse.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var getContentResult = await getContentResponse.Content.ReadAsStringAsync();
                            fileInfo = JsonConvert.DeserializeObject<ContentResponse>(getContentResult);
                        }

                        if (fileInfo == null)
                            return;

                        //Remove file if exist
                        var payload = new ContentPayload()
                        {
                            message = "Delete SonarCloud config file.",
                            sha = fileInfo != null ? fileInfo.sha : "",
                        };
                        var payloadJson = JsonConvert.SerializeObject(payload);
                        var content = new StringContent(payloadJson);
                        HttpRequestMessage httpMessage = new HttpRequestMessage()
                        {
                            Method = HttpMethod.Delete,
                            RequestUri = new Uri($"/repos/{owner}/{repoName}/contents/{fileName}", UriKind.Relative),
                            Content = content
                        };
                        var response = await client.SendAsync(httpMessage);
                        //if (response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
                        //    MessageBox.Show("Configuration file was pushed successfully.", "Push", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    await Task.Delay(50);
                }
                MessageBox.Show("Configuration file(s) was deleted successfully.", "Push", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async Task<string> FindParentRepositoryPath(string repoUrl)
        {
            string originalRepoURL = string.Empty;
            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
            {
                //Get parent if forked is true
                var response = await client.GetAsync($"/repos/{repoUrl}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var repositoryDetails = JsonConvert.DeserializeObject<RepositoryDetails>(jsonString);

                    //Find original source path
                    do
                    {
                        if (repositoryDetails.fork)
                        {
                            if (repositoryDetails.parent.fork)
                            {

                                var innerResponse = await client.GetAsync($"/repos/{repositoryDetails.parent.full_name}");
                                if (innerResponse.IsSuccessStatusCode)
                                {
                                    var innerJsonString = await innerResponse.Content.ReadAsStringAsync();
                                    repositoryDetails = JsonConvert.DeserializeObject<RepositoryDetails>(innerJsonString);

                                    //If repository is not forked we set the original repo url and then  while loop will be closed.
                                    originalRepoURL = repositoryDetails.full_name;
                                }
                                else
                                    throw new Exception("Cannot find the repository!");
                            }
                            else
                            {
                                originalRepoURL = repositoryDetails.parent.full_name;
                                break;
                            }
                        }
                        else
                        {
                            originalRepoURL = repositoryDetails.full_name;
                            break;
                        }


                    } while (repositoryDetails.fork);
                }
                else
                    throw new Exception("Cannot find the repository!");
            }
            return originalRepoURL;
        }

        private async Task<FileSearchResponse> SearchFileWithinRepo(string repoUrl, string filename)
        {
            bool isRemaining = false;
            FileSearchResponse returnResult = new FileSearchResponse() { items = new List<FileItem>() };
            int page = 1;

            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
            {
                string originalRepoURL = await FindParentRepositoryPath(repoUrl);
                //Find file in source path
                //There is some limitation in search which is set by Github and it was explained in the following link
                //https://docs.github.com/en/search-github/searching-on-github/searching-code#considerations-for-code-search

                //The following limitation can affect my result:
                // Only repositories that have had activity or have been returned in search results in the last year are searchable.
                do
                {
                    var searchURL = $"/search/code?q=filename:{filename}+repo:{originalRepoURL}&per_page=100&page={page}";
                    var findResponse = await client.GetAsync(searchURL);

                    //If reach the limit wait for a minute and resend the request
                    if (!findResponse.IsSuccessStatusCode)
                    {
                        //var retryAfter = findResponse.Headers.RetryAfter;
                        //if (retryAfter != null && retryAfter.Delta.HasValue)
                        //    await Task.Delay(retryAfter.Delta.Value);
                        //else
                        await Task.Delay(TimeSpan.FromMinutes(1));
                        findResponse = await client.GetAsync(searchURL);
                    }


                    if (findResponse.IsSuccessStatusCode)
                    {
                        string jsonResult = await findResponse.Content.ReadAsStringAsync();
                        FileSearchResponse result = JsonConvert.DeserializeObject<FileSearchResponse>(jsonResult);
                        //Extract the actual path and remove FileName at the end of path that Github returns
                        foreach (var fileItem in result.items)
                        {
                            int removeStringLength = 1 + filename.Length; // 1 for [/] + filename length => for instance: /AndroidManifest.xml should be remove from the path
                            string extractedSourcePath = string.Empty;
                            if (fileItem.path.Length - removeStringLength > 0) //Prevent errors if the file is on the Root directory of the repository
                                extractedSourcePath = fileItem.path.Remove(fileItem.path.Length - removeStringLength, removeStringLength);
                            fileItem.path = extractedSourcePath;
                        }

                        returnResult.incomplete_results = result.incomplete_results;
                        returnResult.total_count = result.total_count;
                        returnResult.items.AddRange(result.items);

                        isRemaining = result.total_count > (page * 100);

                        if (page < 10)
                            page++;
                        else
                            isRemaining = false;
                    }
                    else
                    {
                        throw new Exception("Cannot find the specified file.");
                    }


                } while (isRemaining);

                return returnResult;

            }
        }

        private void ClearResult()
        {
            RepoListBox.DataSource = null;
            RepoListBox.Items.Clear();
            ResultsCountLabel.Text = "";
            TotalItemFoundLabel.Text = "";
            SelectedCountLabel.Text = "";
        }

        private async void FindFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                if (RepoListBox.SelectedItem is RepoListBoxItem repoListBoxItem)
                {
                    string fileName = string.Empty;
                    TargetFramework targetFramework = TargetFramework.Unknown;
                    if (FindFileAndroidRadioButton.Checked)
                    {
                        fileName = "AndroidManifest.xml";
                        targetFramework = TargetFramework.AndroidNativeSDK_Kotlin;
                    }
                    else if (FindFileiOSRadioButton.Checked)
                    {
                        fileName = "Info.plist";
                        targetFramework = TargetFramework.iOSNativeSDK_Swift;
                    }
                    else if (FindFileReactNativeRadioButton.Checked)
                    {
                        fileName = "Package.json";
                        targetFramework = TargetFramework.ReactNative;
                    }
                    else if (FindFileCustomRadioButton.Checked)
                    {
                        if (string.IsNullOrWhiteSpace(FileNameTextBox.Text))
                        {
                            MessageBox.Show("File name cannot be empty.", "Find File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        fileName = FileNameTextBox.Text;
                    }
                    var file = await SearchFileWithinRepo(repoListBoxItem.Repo.full_name, fileName);
                    if (file.total_count > 0)
                    {
                        string sourceCodefilesPath = string.Empty;
                        foreach (var fileItem in file.items)
                        {
                            sourceCodefilesPath += $"/{fileItem.path},";
                        }
                        //remove last comma [,] from the paths
                        sourceCodefilesPath = sourceCodefilesPath.TrimEnd(',');

                        string message = $"Sarch Complete, {file.total_count} item(s) found! {Environment.NewLine}" +
                                         $"Path(s): {Environment.NewLine}";
                        file.items.ForEach(item => message += item.path + Environment.NewLine);

                        MessageBox.Show(message, "Find File", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        ProjectPathLabel.Text = sourceCodefilesPath;
                        repoListBoxItem.Framework = targetFramework;
                        repoListBoxItem.SourceCodePath = sourceCodefilesPath;
                    }
                    else
                        MessageBox.Show("Cannot find the file.", "Find File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                    MessageBox.Show("You must select a repository.", "Find File", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void FindFileCustomRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            FileNameLabel.Enabled = FileNameTextBox.Enabled = FindFileCustomRadioButton.Checked;
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            ProjectPathLabel.Text = FileNameTextBox.Text = string.Empty;
        }

        private async void ValidationResultButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                string fileName = ValidateOnFileNameTextBox.Text;

                if (string.IsNullOrWhiteSpace(fileName))
                    return;

                foreach (RepoListBoxItem item in RepoListBox.SelectedItems)
                {
                    //The search method internally handle API rate limit
                    //TODO: this rate limitation should be handeled by a http client or a http pipline

                    //Bypass the valid result for next evaluation if it's needed
                    if (item.IsValidApplicationRepo)
                        continue;

                    try
                    {
                        await semaphore.WaitAsync();
                        var file = await SearchFileWithinRepo(item.Repo.full_name, fileName);
                        if (file.total_count > 0)
                        {
                            string sourceCodefilesPath = string.Empty;
                            bool findPath = false;
                            //The logic of finding the path of source files is different for each framework 
                            switch (item.Framework)
                            {
                                case TargetFramework.AndroidNativeSDK_Kotlin:
                                    {
                                        foreach (var fileItem in file.items)
                                            sourceCodefilesPath += $"/{fileItem.path},";
                                        //remove last comma [,] from the paths
                                        sourceCodefilesPath = sourceCodefilesPath.TrimEnd(',');
                                        findPath = true;
                                        break;
                                    }
                                case TargetFramework.iOSNativeSDK_Swift:
                                    {
                                        foreach (var fileItem in file.items)
                                        {
                                            if (fileItem.path.EndsWith("/Assets.xcassets"))
                                            {
                                                findPath = true;
                                                string removedPart = "/Assets.xcassets";
                                                sourceCodefilesPath = fileItem.path.Remove(fileItem.path.Length - removedPart.Length, removedPart.Length) + ",";
                                            }
                                        }
                                        sourceCodefilesPath = sourceCodefilesPath.TrimEnd(',');
                                        break;
                                    }
                                case TargetFramework.ReactNative:
                                    {
                                        // I don't want to extract the source path at this point, because I will apply same inclusion/exclusion logic for all react-native projects
                                        findPath = true;
                                        break;
                                    }
                                case TargetFramework.Unknown:
                                    break;
                                default:
                                    break;
                            }


                            if (findPath)
                            {
                                item.IsValidApplicationRepo = true;
                                item.DisplayText = "✔ " + item.DisplayText;
                                item.SourceCodePath = sourceCodefilesPath;
                            }
                            else
                            {
                                item.IsValidApplicationRepo = false;
                                item.DisplayText = "❌ " + item.DisplayText;
                                item.SourceCodePath = sourceCodefilesPath;
                            }
                        }
                        else
                        {
                            item.IsValidApplicationRepo = false;
                            item.DisplayText = "❌ " + item.DisplayText;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                RefreshListBox();
                MessageBox.Show("Validation process is completed.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void TargetFrameworkCombobox_SelectedValueChanged(object sender, EventArgs e)
        {
            TargetFramework targetFramework = TargetFramework.Unknown;
            switch (TargetFrameworkCombobox.SelectedIndex)
            {
                case 0:
                    targetFramework = TargetFramework.AndroidNativeSDK_Kotlin;
                    ValidateOnFileNameTextBox.Text = "AndroidManifest.xml";
                    break;
                case 1:
                    targetFramework = TargetFramework.iOSNativeSDK_Swift;
                    ValidateOnFileNameTextBox.Text = "Contents.json";
                    break;
                case 2:
                    targetFramework = TargetFramework.ReactNative;
                    ValidateOnFileNameTextBox.Text = "Package.json";
                    break;
                case 3:
                    targetFramework = TargetFramework.Unknown;
                    break;
            }

            foreach (RepoListBoxItem item in RepoListBox.Items)
                item.Framework = targetFramework;
        }

        private void RefreshListBox()
        {
            var castedList = RepoListBox.Items.Cast<RepoListBoxItem>();
            var newList = castedList.Select(a => ((ICloneable)a).Clone()).ToList();
            RepoListBox.Items.Clear();
            foreach (var item in newList)
            {
                var index = RepoListBox.Items.Count - 1;
                RepoListBox.Items.Add(item);
            }
        }

        private void SelectValidRepo_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < RepoListBox.Items.Count; i++)
                if (RepoListBox.Items[i] is RepoListBoxItem item)
                    RepoListBox.SetSelected(i, item.IsValidApplicationRepo);
        }

        private async void GetOrgButton_Click(object sender, EventArgs e)
        {
            await FillUserOrganizations();
        }

        public async Task FillUserOrganizations()
        {
            using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
            {
                var jsonResponse = await client.GetStringAsync("/user/orgs");
                List<Organization> orgs = JsonConvert.DeserializeObject<List<Organization>>(jsonResponse);
                UserHelper.User.Organizations = orgs;
                MyOrganizationCombobox.DataSource = UserHelper.User.Organizations;
                MyOrganizationCombobox.DisplayMember = "login";
                MyOrganizationCombobox.ValueMember = "login";
            }
        }

        private async void GetListOfOrganizationRepositoryButton_Click(object sender, EventArgs e)
        {
            try
            {
                ClearResult();
                IsInProgress = true;
                using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                {
                    var pageCount = 1;
                    bool remainingItem = true;
                    while (remainingItem)
                    {
                        if (!IsInProgress)
                            break;

                        var response = await client.GetAsync($"/orgs/{UserHelper.User.SelectedOrganization}/repos?per_page=100&page={pageCount}");
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            var repoList = JsonConvert.DeserializeObject<List<Repository>>(result);
                            FillRepoListBox(repoList, false);
                            remainingItem = (repoList.Count > 0);
                            pageCount++;
                            await Task.Delay(50);
                        }
                        else
                            remainingItem = false;
                    }

                }
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async Task<List<Repository>> GetAccountRepositories()
        {
            try
            {
                using (HttpClient client = HttpClientHelper.CreateGitHubHttpClient(UserHelper.User.Token))
                {
                    var pageCount = 1;
                    bool remainingItem = true;
                    List<Repository> returnRepoList = new List<Repository>();
                    while (remainingItem)
                    {
                        if (!IsInProgress)
                            break;

                        var response = await client.GetAsync($"/orgs/{UserHelper.User.SelectedOrganization}/repos?per_page=100&page={pageCount}");
                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            var repos = JsonConvert.DeserializeObject<List<Repository>>(result);
                            returnRepoList.AddRange(repos);
                            remainingItem = (repos.Count > 0);
                            pageCount++;
                            await Task.Delay(50);
                        }
                        else
                            remainingItem = false;
                    }
                    return returnRepoList;
                }
            }
            finally
            {
            }
        }

        private void MyOrganizationCombobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MyOrganizationCombobox.SelectedItem is Organization org)
            {
                if (!string.IsNullOrWhiteSpace(org.login))
                {
                    GetListOfOrganizationRepositoryButton.Enabled = true;
                    UserHelper.User.SelectedOrganization = org.login;
                    UserNameLabel.Text = $"{UserHelper.User.Username} ({UserHelper.User.SelectedOrganization})";
                }
                else
                {
                    GetListOfOrganizationRepositoryButton.Enabled = false;
                    UserHelper.User.SelectedOrganization = string.Empty;
                }
            }
        }

        private void SelectInValidRepoButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < RepoListBox.Items.Count; i++)
                if (RepoListBox.Items[i] is RepoListBoxItem item)
                    RepoListBox.SetSelected(i, !item.IsValidApplicationRepo);
        }

        private void RepoListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (RepoListBox.SelectedItems.Count > 0)
                SelectedCountLabel.Text = $"Selected Items:{RepoListBox.SelectedItems.Count}";
            else
                SelectedCountLabel.Text = "";
        }

        private byte[] GenereteSonarCloudConfigFileContent(string path, TargetFramework framework)
        {
            switch (framework)
            {
                case TargetFramework.AndroidNativeSDK_Kotlin:
                    return GenerateAndroidNativeSonarConfigFileContent(path);
                case TargetFramework.iOSNativeSDK_Swift:
                    return GenerateiOSNativeSonarConfigFileContent(path);
                case TargetFramework.ReactNative:
                    return GenerateReactNativeSonarConfigFileContent();
                default:
                case TargetFramework.Unknown:
                    break;
            }
            return new byte[0];
        }

        private byte[] GenerateAndroidNativeSonarConfigFileContent(string path)
        {
            string sonarCloudConfigFileContent = "sonar.exclusions=**/*.kt, **/*.xml,**/*.XML,**/*.Xml,**/*.KT,**/*.jsx,**/*.tsx, **/*.XML,**/*.css,**/*.CSS,**/*.less,**/*.scss,**/*.sass,**/*.js,**/*.Js,**/*.jS,**/*.JS,**/*.ts,**/*.Ts,**/*.tS,**/*.TS,**/*.json,**/*.config.js,**/*.html,**/*.HTML,**/*.xhtml,**/*.cs,**/*.c,**/*.h,**/*.cc,**/*.cpp,**/*.cxx,**/*.c++,**/*.hh,**/*.hpp,**/*.hxx,**/*.h++,**/*.ipp,**/*.m,**/*.sql,**/*.tab,**/*.pkb,**/*.vb,**/*.py,**/*.xsd,**/*.go,**/*.php,**/*.GO,**/*.PHP,**/*.rb,**/*.scala,**/*.jsp";
            sonarCloudConfigFileContent += Environment.NewLine;
            sonarCloudConfigFileContent += "sonar.inclusions=";

            foreach (var sourcePathItem in path.Split(','))
            {
                string content = string.Empty;
                if (sourcePathItem == "/" || string.IsNullOrWhiteSpace(sourcePathItem))
                    sonarCloudConfigFileContent += $"**/*.java,";
                else
                    sonarCloudConfigFileContent += $"{sourcePathItem}/**/*.java,";
            }
            sonarCloudConfigFileContent = sonarCloudConfigFileContent.TrimEnd(',');
            return Encoding.UTF8.GetBytes(sonarCloudConfigFileContent);
        }

        private byte[] GenerateiOSNativeSonarConfigFileContent(string path)
        {
            string sonarCloudConfigFileContent = "sonar.inclusions = ";
            foreach (var sourcePathItem in path.Split(','))
            {
                string content = string.Empty;
                if (path == "/" || string.IsNullOrWhiteSpace(path))
                    sonarCloudConfigFileContent += $"**/*.swift,";
                else
                    sonarCloudConfigFileContent += $"{path}/**/*.swift,";
            }
            sonarCloudConfigFileContent = sonarCloudConfigFileContent.TrimEnd(',');
            return Encoding.UTF8.GetBytes(sonarCloudConfigFileContent);
        }

        private byte[] GenerateReactNativeSonarConfigFileContent()
        {

            string sonarCloudConfigFileContent = "sonar.exclusions = **/*node_modules,/**/*config.js" +
                                                 Environment.NewLine +
                                                 "sonar.inclusions = /**/*.js,/**/*.ts,/**/*.tsx";
            return Encoding.UTF8.GetBytes(sonarCloudConfigFileContent);
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (RepoListBox.Items.Count == 0)
                return;

            List<RepoListBoxItem> repoList = new List<RepoListBoxItem>();
            foreach (var item in RepoListBox.Items)
            {

                var repoItem = item as RepoListBoxItem;
                repoList.Add(repoItem);
            }

            var jsonString = JsonConvert.SerializeObject(repoList);
            var content = Encoding.UTF8.GetBytes(jsonString);

            var savePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\Downloads\" + DateTime.Now.Ticks + ".json";
            using (StreamWriter sw = new StreamWriter(savePath))
                sw.Write(jsonString);

            MessageBox.Show($"The result was saved in the following path: {savePath}", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.Filter = "Json files (*.json)|*.json";
                dialog.DefaultExt = ".json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var jsonString = "";
                        using (StreamReader sr = new StreamReader(dialog.FileName))
                            jsonString = sr.ReadLine();

                        var repoList = JsonConvert.DeserializeObject<List<RepoListBoxItem>>(jsonString);
                        if (repoList.Count > 0)
                        {
                            RepoListBox.Items.Clear();

                            foreach (var item in repoList)
                                RepoListBox.Items.Add(item);

                            RepoListBox.DisplayMember = "DisplayText";
                            RepoListBox.ValueMember = "Repo";
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private void FindButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                string searchText = SearchTextBox.Text.ToLower();
                if (string.IsNullOrWhiteSpace(searchText))
                    return;

                for (int i = 0; i < RepoListBox.Items.Count; i++)
                    if (RepoListBox.Items[i] is RepoListBoxItem item)
                        if (item.Repo.name.ToLower().Contains(searchText))
                            RepoListBox.SetSelected(i, true);
            }
            finally
            {
                IsInProgress = false;
            }

        }

        private async void GetListOfSonarCloudProjectsButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserHelper.User.SelectedOrganization))
            {
                MessageBox.Show("To fetch projects from the SonarCloud, you must first choose the organization.", "Select the Organization", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsInProgress = true;
                using (HttpClient client = HttpClientHelper.CreateSonarCloudHttpClient())
                {
                    var response = await client.GetAsync($"components/search?organization={UserHelper.User.SelectedOrganization.ToLower()}&ps=500&p=1");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResult = await response.Content.ReadAsStringAsync();
                        var result = JsonConvert.DeserializeObject<ProjectListResponse>(jsonResult);
                        projectList.AddRange(result.components);
                        for (int i = 2; i <= (result.paging.total / 500) + 1; i++)
                        {
                            response = await client.GetAsync($"components/search?organization={UserHelper.User.SelectedOrganization.ToLower()}&ps=500&p={i}");
                            if (response.IsSuccessStatusCode)
                            {
                                jsonResult = await response.Content.ReadAsStringAsync();
                                result = JsonConvert.DeserializeObject<ProjectListResponse>(jsonResult);
                                projectList.AddRange(result.components);
                            }
                        }
                    }
                }
                MessageBox.Show($"{projectList.Count} item(s) is/are received.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ComponentNumberLabel.Text = $"{projectList.Count} item(s) is/are found.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to recieve information from the SonarCloud, Message: {ex}", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private async void GetProjectMeasuresButton_Click(object sender, EventArgs e)
        {
            if (projectList.Count == 0)
            {
                MessageBox.Show("You did not fetch projects from the SonarCloud yet.", "Fetch projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsInProgress = true;
                using (HttpClient client = HttpClientHelper.CreateSonarCloudHttpClient())
                {
                    var metricsKeys = "bugs,code_smells,sqale_index,duplicated_lines_density,duplicated_lines,duplicated_blocks,duplicated_files,ncloc,lines,statements,functions,classes,files,complexity,cognitive_complexity";
                    foreach (var item in projectList)
                    {
                        var response = await client.GetAsync($"measures/component_tree?component={item.key}&metricKeys={metricsKeys}&ps=1&p=1");
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResult = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<MeasureResponse>(jsonResult);

                            var report = new ReportResult();
                            report.ProjectName = item.name;
                            foreach (var measureItem in result.baseComponent.measures)
                                report.MetircsValues.Add(measureItem.metric, measureItem.value);

                            metricResultList.Add(report);
                        }
                    }
                }
                MessageBox.Show($"{metricResultList.Count} item(s) is/are received.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReportStatusLabel.Text = $"{metricResultList.Count} record(s) is/are generated.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to recieve information from the SonarCloud, Message: {ex}", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }
        private void GenerateReportButton_Click(object sender, EventArgs e)
        {
            if (metricResultList.Count == 0)
            {
                MessageBox.Show("You did not get the projects measures from the SonarCloud yet.", "Cannot generate report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsInProgress = true;
                List<ReportRecord> reportRecordList = new List<ReportRecord>();
                foreach (var item in metricResultList)
                {
                    if (item.MetircsValues.Count < 15)
                        continue;

                    var record = new ReportRecord();
                    record.Name = item.ProjectName;
                    record.Bugs = int.Parse(item.MetircsValues["bugs"]);
                    record.CodeSmell = int.Parse(item.MetircsValues["code_smells"]);
                    record.Debt = item.MetircsValues["sqale_index"];
                    record.Duplication = item.MetircsValues["duplicated_lines_density"];
                    record.DuplicatedLines = int.Parse(item.MetircsValues["duplicated_lines"]);
                    record.DuplicatedBlocks = int.Parse(item.MetircsValues["duplicated_blocks"]);
                    record.DuplicatedFiles = int.Parse(item.MetircsValues["duplicated_files"]);
                    record.LinesOfCode = int.Parse(item.MetircsValues["ncloc"]);
                    record.TotalLines = int.Parse(item.MetircsValues["lines"]);
                    record.NumberOfStatements = int.Parse(item.MetircsValues["statements"]);
                    record.NumberOfFunctions = int.Parse(item.MetircsValues["functions"]);
                    record.NumberOfClasses = int.Parse(item.MetircsValues["classes"]);
                    record.NumberOfFiles = int.Parse(item.MetircsValues["files"]);
                    record.CyclomaticComplexity = int.Parse(item.MetircsValues["complexity"]);
                    record.CognitiveComplexity = int.Parse(item.MetircsValues["cognitive_complexity"]);
                    reportRecordList.Add(record);
                }


                var savePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + $@"\Downloads\{UserHelper.User.SelectedOrganization}.csv";
                using (var writer = new StreamWriter(savePath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(reportRecordList);
                }

                MessageBox.Show($"Report file containing {reportRecordList.Count} item(s) is generated.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FinalStatusReportLabel.Text = $"Report file containing {reportRecordList.Count} item(s) is generated and is saved in the following path:{Environment.NewLine} {savePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate report, Message: {ex}", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void ClearReportButton_Click(object sender, EventArgs e)
        {
            projectList.Clear();
            metricResultList.Clear();
            issueList.Clear();
            ComponentNumberLabel.Text = "";
            ReportStatusLabel.Text = "";
            ReportStatusLabel.Text = "";
            FinalStatusReportLabel.Text = "";
        }

        private void CopyItemAddressButton_Click(object sender, EventArgs e)
        {
            if (RepoListBox.SelectedItems.Count > 0)
                if (RepoListBox.SelectedItems[0] is RepoListBoxItem item)
                    Clipboard.SetText(item.Repo.svn_url);
        }

        private async void ExtractAndroidIssues_Click(object sender, EventArgs e)
        {
            if (projectList.Count == 0)
            {
                MessageBox.Show("You did not fetch projects from the SonarCloud yet.", "Fetch projects", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                IsInProgress = true;
                using (HttpClient client = HttpClientHelper.CreateSonarCloudHttpClient())
                {
                    foreach (var item in projectList)
                    {
                        var response = await client.GetAsync($"issues/search?projects={item.key}&ps=500&p=1");
                        if (response.IsSuccessStatusCode)
                        {
                            var jsonResult = await response.Content.ReadAsStringAsync();
                            var result = JsonConvert.DeserializeObject<IssueResponse>(jsonResult);

                            if (result.total == 0)
                                continue;

                            issueList.AddRange(result.issues);
                            for (int i = 1; i < (result.total / 500) + 1; i++)
                            {
                                response = await client.GetAsync($"issues/search?projects={item.key}&ps=500&p={i + 1}");
                                result = JsonConvert.DeserializeObject<IssueResponse>(jsonResult);
                                issueList.AddRange(result.issues);
                            }
                        }
                    }
                }
                MessageBox.Show($"{issueList.Count} issue item(s) is/are received.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void ExtractBugsButtons_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                var list = GetIssueReport("BUG");
                if (list.Count() == 0)
                {
                    MessageBox.Show($"Could not find any results.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var savePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + $@"\Downloads\Bugs-{DateTime.Now.Ticks}.csv";
                ExportCSVFile(list, savePath);
                MessageBox.Show($"Report file is generated and is saved in the following path:{Environment.NewLine} {savePath}.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private void ExtractCodeSmellButton_Click(object sender, EventArgs e)
        {
            try
            {
                IsInProgress = true;
                var list = GetIssueReport("CODE_SMELL");
                if (list.Count() == 0)
                {
                    MessageBox.Show($"Could not find any results.", "Failed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var savePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + $@"\Downloads\CodeSmell-{DateTime.Now.Ticks}.csv";
                ExportCSVFile(list, savePath);
                MessageBox.Show($"Report file is generated and is saved in the following path:{Environment.NewLine} {savePath}.", "Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                IsInProgress = false;
            }
        }

        private IEnumerable<ExportIssueReport> GetIssueReport(string type)
        {
            var groupedList = issueList
                                .Where(a => a.type == type)
                                .GroupBy(a => a.message)
                                .OrderByDescending(group => group.Count())
                                .Select(g => new ExportIssueReport() { Message = g.Key, Count = g.Count() })
                                .ToList();
            return groupedList;
        }

        private void ExportCSVFile(IEnumerable<object> list, string path)
        {
            using (var writer = new StreamWriter(path))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(list);
            }
        }

    }

    public class RepoListBoxItem : ICloneable
    {
        public Repository Repo { get; set; }
        public string DisplayText { get; set; }
        public string SourceCodePath { get; set; }
        public TargetFramework Framework { get; set; }
        public bool IsValidApplicationRepo { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public enum TargetFramework
    {
        AndroidNativeSDK_Kotlin,
        iOSNativeSDK_Swift,
        ReactNative,
        Unknown
    }

    public class ExportIssueReport
    {
        public string Message { get; set; }
        public int Count { get; set; }
    }
}
