using System;
using Windows.UI.Popups;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Input;
using Windows.Storage;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json.Schema;
using System.Net;
using Windows.Web.Syndication;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace FeedReader
{
    public class Settings
    {

    }

    public class FeedFolder
    {
        public string Id { get; set; }
        public string Title { get; set; }

        public static FeedFolder New(string title)
        {
            return new FeedFolder() { Id = Guid.NewGuid().ToString(), Title = title };
        }
    }

    public class Feed
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Copyright { get; set; }
        public string Language { get; set; }
        public string IconUrl { get; set; }

        public string FolderId { get; set; }

        public static Feed New(string url)
        {
            Feed newFeed = new() { Url = url };

            SyndicationFeed feed = new();

            using (WebClient client = new())
            {
                string feedStr = client.DownloadString(url);
                feed.Load(feedStr);
            }

            newFeed.Id = url;  // TODO: Get the ID from the XML
            newFeed.Title = feed.Title.Text;
            newFeed.Link = feed.Links[0].Uri.ToString();
            if (feed.Subtitle != null)
            {
                newFeed.Description = feed.Subtitle.Text;
            }
            else
            {
                newFeed.Description = "";
            }
            newFeed.LastUpdated = feed.LastUpdatedTime.UtcDateTime;
            if (feed.Rights != null)
            {
                newFeed.Copyright = feed.Rights.Text;
            }
            else
            {
                newFeed.Copyright = "";
            }
            newFeed.Language = feed.Language;
            newFeed.IconUrl = "http://www.google.com/s2/favicons?domain=" + newFeed.Link;

            return newFeed;
        }
    }

    public class FeedEntry
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Link { get; set; }
        public DateTime Published { get; set; }
        public string[] Links { get; set; }
        public string[] Authors { get; set; }
        public string[] Categories { get; set; }

        public DateTime Received { get; set; }
        public bool Read { get; set; } = false;
        public bool Hidden { get; set; } = false;

        public string FeedId { get; set; }

        public static FeedEntry New(SyndicationItem item, string feedId)
        {
            FeedEntry newFeedEntry = new()
            {
                Id = item.Id != null ? item.Id : item.Links[0].ToString(),
                Title = item.Title.Text,
                Summary = item.Summary != null ? item.Summary.Text : "",
                Link = item.Links[0].ToString(),
                Published = item.PublishedDate.UtcDateTime,
                Links = new string[item.Links.Count],
                Authors = new string[item.Authors.Count],
                Categories = new string[item.Categories.Count],
                Received = DateTime.Now,
                Read = false,
                Hidden = false,
                FeedId = feedId
            };

            foreach (var link in item.Links)
            {
                newFeedEntry.Links.Append(link.ToString());
            }
            foreach (var author in item.Authors)
            {
                newFeedEntry.Authors.Append(author.Name);
            }
            foreach (var category in item.Categories)
            {
                newFeedEntry.Categories.Append(category.Label);
            }

            return newFeedEntry;
        }

        public static FeedEntry Dummy()
        {
            return new FeedEntry()
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Title",
                Summary = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce interdum tortor quis metus aliquam, eget vehicula felis iaculis. Curabitur in ante bibendum, finibus ex a, venenatis libero. In vel neque.",
                Link = "https://www.google.com",
                Published = DateTime.Now,
                Links = new string[0],
                Authors = new string[0],
                Categories = new string[0],
                Received = DateTime.Now,
                Read = false,
                Hidden = false,
                FeedId = "F in the chat"
            };
        }

        public string GetTimeTillPublished()
        {
            TimeSpan diff = DateTime.Now - Published;

            if (diff.TotalSeconds <= 3)
            {
                return "Now";
            }
            else if (diff.TotalSeconds < 60)
            {
                return (int)diff.TotalSeconds + " s";
            }
            else if (diff.TotalMinutes < 60)
            {
                return (int)diff.TotalMinutes + " min";
            }
            else if (diff.TotalHours < 24)
            {
                return (int)diff.TotalHours + " hrs";
            }
            else if (diff.TotalDays < 30)
            {
                return (int)diff.TotalDays + " days";
            }
            else if (diff.TotalDays < 365)
            {
                return (int)(diff.TotalDays / 30) + " mon";
            }
            else
            {
                return (int)(diff.TotalDays / 365) + " yrs";
            }
        }

        public string GetTimeTillReceived()
        {
            TimeSpan diff = DateTime.Now - Received;

            if (diff.TotalSeconds <= 3)
            {
                return "Now";
            }
            else if (diff.TotalSeconds < 60)
            {
                return (int)diff.TotalSeconds + " s";
            }
            else if (diff.TotalMinutes < 60)
            {
                return (int)diff.TotalMinutes + " min";
            }
            else if (diff.TotalHours < 24)
            {
                return (int)diff.TotalHours + " hrs";
            }
            else if (diff.TotalDays < 30)
            {
                return (int)diff.TotalDays + " days";
            }
            else if (diff.TotalDays < 365)
            {
                return (int)(diff.TotalDays / 30) + " mon";
            }
            else
            {
                return (int)(diff.TotalDays / 365) + " yrs";
            }
        }

        public string GetPublishedAndReceivedStr()
        {
            return "Published: " + Published.ToLongDateString() + "\nReceived: " + Received.ToLongDateString();
        }
    }

    public static class DataAccess
    {
        public static string DbFileName = "database.db";

        public async static void InitDb()
        {
            await ApplicationData.Current.LocalFolder.CreateFileAsync(DbFileName, CreationCollisionOption.OpenIfExists);
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            string cmd = "PRAGMA foreign_keys = ON;";
            new SqliteCommand(cmd, conn).ExecuteNonQuery();

            cmd = "CREATE TABLE IF NOT EXISTS FeedFolder (Id TEXT PRIMARY KEY, Title TEXT);";
            new SqliteCommand(cmd, conn).ExecuteNonQuery();

            cmd = "CREATE TABLE IF NOT EXISTS Feed (Id TEXT PRIMARY KEY, Url TEXT, Title TEXT, Link TEXT, Description TEXT, LastUpdated NUMERIC, Copyright TEXT, Language TEXT, IconUrl TEXT, FolderId, FOREIGN KEY(FolderId) REFERENCES FeedFolder(Id) ON DELETE CASCADE);";
            new SqliteCommand(cmd, conn).ExecuteNonQuery();

            cmd = "CREATE TABLE IF NOT EXISTS FeedEntry (Id TEXT PRIMARY KEY, Title TEXT, Summary TEXT, Link TEXT, Published NUMERIC, Received NUMERIC, Read BOOL, Hidden BOOL, FeedId TEXT, FOREIGN KEY(FeedId) REFERENCES Feed(Id) ON DELETE CASCADE);";
            new SqliteCommand(cmd, conn).ExecuteNonQuery();
        }

        // folder functions
        public static void AddFeedFolder(FeedFolder feedFolder)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = "INSERT INTO FeedFolder VALUES ($Id, $Title);"
            };

            cmd.Parameters.AddWithValue("$Id", feedFolder.Id);
            cmd.Parameters.AddWithValue("$Title", feedFolder.Title);

            cmd.ExecuteNonQuery();
        }

        public static FeedFolder GetFeedFolder(string id)
        {
            List<FeedFolder> feedFolders = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedFolder WHERE Id = \"{id}\""
            };
            var query = cmd.ExecuteReader();

            while (query.Read())
            {
                feedFolders.Add(new FeedFolder()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1)
                });
            }

            return feedFolders[0];
        }

        public static List<FeedFolder> GetFeedFolders()
        {
            List<FeedFolder> feedFolders = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = "SELECT * FROM FeedFolder"
            };
            var query = cmd.ExecuteReader();

            while (query.Read())
            {
                feedFolders.Add(new FeedFolder()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1)
                });
            }

            return feedFolders;
        }

        public static void UpdateFeedFolder(FeedFolder feedFolder)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedFolder SET Title = \"{feedFolder.Title}\" WHERE Id = \"{feedFolder.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void DeleteFeedFolder(FeedFolder feedFolder)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"DELETE FROM FeedFolder WHERE Id = \"{feedFolder.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void DeleteFeedFolder(string feedFolderId)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"DELETE FROM FeedFolder WHERE Id = \"{feedFolderId}\";"
            };

            cmd.ExecuteNonQuery();
        }

        // feed functions
        public static void AddFeed(Feed feed, FeedFolder feedFolder)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = "INSERT INTO Feed VALUES ($Id, $Url, $Title, $Link, $Description, $LastUpdated, $Copyright, $Language, $IconUrl, $FolderId);"
            };

            cmd.Parameters.AddWithValue("$Id", feed.Id);
            cmd.Parameters.AddWithValue("$Url", feed.Url);
            cmd.Parameters.AddWithValue("$Title", feed.Title);
            cmd.Parameters.AddWithValue("$Link", feed.Link);
            cmd.Parameters.AddWithValue("$Description", feed.Description);
            cmd.Parameters.AddWithValue("$LastUpdated", Utils.DateTimeToTimestamp(feed.LastUpdated, true));
            cmd.Parameters.AddWithValue("$Copyright", feed.Copyright);
            cmd.Parameters.AddWithValue("$Language", feed.Language);
            cmd.Parameters.AddWithValue("$IconUrl", feed.IconUrl);
            cmd.Parameters.AddWithValue("$FolderId", feedFolder.Id);

            cmd.ExecuteNonQuery();
        }

        public static Feed GetFeed(string id)
        {
            List<Feed> feeds = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM Feed WHERE Id = \"{id}\""
            };
            var query = cmd.ExecuteReader();

            while (query.Read())
            {
                feeds.Add(new Feed()
                {
                    Id = query.GetString(0),
                    Url = query.GetString(1),
                    Title = query.GetString(2),
                    Link = query.GetString(3),
                    Description = query.GetString(4),
                    LastUpdated = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Copyright = query.GetString(6),
                    Language = query.GetString(7),
                    IconUrl = query.GetString(8),
                    FolderId = query.GetString(9)
                });
            }

            return feeds[0];
        }

        public static List<Feed> GetFeeds(FeedFolder feedFolder)
        {
            List<Feed> feeds = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM Feed WHERE FolderId = \"{feedFolder.Id}\""
            };
            var query = cmd.ExecuteReader();

            while (query.Read())
            {
                feeds.Add(new Feed()
                {
                    Id = query.GetString(0),
                    Url = query.GetString(1),
                    Title = query.GetString(2),
                    Link = query.GetString(3),
                    Description = query.GetString(4),
                    LastUpdated = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Copyright = query.GetString(6),
                    Language = query.GetString(7),
                    IconUrl = query.GetString(8),
                    FolderId = feedFolder.Id,
                });
            }

            return feeds;
        }

        public static void UpdateFeed(Feed feed)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE Feed SET Title = \"{feed.Title}\", Link = \"{feed.Link}\", Description = \"{feed.Description}\", LastUpdated = \"{Utils.DateTimeToTimestamp(feed.LastUpdated, true)}\", Language = \"{feed.Language}\", Copyright = \"{feed.Copyright}\", IconUrl = \"{feed.IconUrl}\" WHERE Id = \"{feed.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void DeleteFeed(Feed feed)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"DELETE FROM Feed WHERE Id = \"{feed.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void DeleteFeed(string feedId)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"DELETE FROM Feed WHERE Id = \"{feedId}\";"
            };

            cmd.ExecuteNonQuery();
        }

        // entry functions
        public static void AddFeedEntries(List<FeedEntry> entries, Feed feed)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            using var transaction = conn.BeginTransaction();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = "INSERT INTO FeedEntry (Id, Title, Summary, Link, Published, Received, Read, Hidden, FeedId)\r\nVALUES ($Id, $Title, $Summary, $Link, $Published, $Received, $Read, $Hidden, $FeedId)\r\nON CONFLICT (Id) DO UPDATE SET Title = EXCLUDED.Title, Summary = EXCLUDED.Summary, Link = EXCLUDED.Link, Published = EXCLUDED.Published, Received = EXCLUDED.Received, FeedId = EXCLUDED.FeedId;\r\n",
                Transaction = transaction
            };

            var idParam = cmd.CreateParameter();
            var titleParam = cmd.CreateParameter();
            var summaryParam = cmd.CreateParameter();
            var linkParam = cmd.CreateParameter();
            var publishedParam = cmd.CreateParameter();
            var receivedParam = cmd.CreateParameter();
            var readParam = cmd.CreateParameter();
            var hiddenParam = cmd.CreateParameter();
            var feedIdParam = cmd.CreateParameter();

            idParam.ParameterName = "$Id";
            titleParam.ParameterName = "$Title";
            summaryParam.ParameterName = "$Summary";
            linkParam.ParameterName = "$Link";
            publishedParam.ParameterName = "$Published";
            receivedParam.ParameterName = "$Received";
            readParam.ParameterName = "$Read";
            hiddenParam.ParameterName = "$Hidden";
            feedIdParam.ParameterName = "$FeedId";

            cmd.Parameters.Add(idParam);
            cmd.Parameters.Add(titleParam);
            cmd.Parameters.Add(summaryParam);
            cmd.Parameters.Add(linkParam);
            cmd.Parameters.Add(publishedParam);
            cmd.Parameters.Add(receivedParam);
            cmd.Parameters.Add(readParam);
            cmd.Parameters.Add(hiddenParam);
            cmd.Parameters.Add(feedIdParam);

            foreach (var entry in entries)
            {
                idParam.Value = entry.Id;
                titleParam.Value = entry.Title;
                summaryParam.Value = entry.Summary;
                linkParam.Value = entry.Link;
                publishedParam.Value = new DateTimeOffset(entry.Published).ToUnixTimeMilliseconds().ToString();
                receivedParam.Value = new DateTimeOffset(entry.Received).ToUnixTimeMilliseconds().ToString();
                readParam.Value = entry.Read;
                hiddenParam.Value = entry.Hidden;
                feedIdParam.Value = feed.Id;

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public static List<FeedEntry> GetFeedEntries(FeedFolder feedFolder, bool includeRead = false, bool includeHidden = false, bool sortPublishedLatest = true)
        {
            List<Feed> feeds = GetFeeds(feedFolder);
            List<FeedEntry> feedEntries = new();

            if (!(feeds.Count > 0))
            {
                return feedEntries;
            }

            string ndny = ""; // Not decided name yet
            foreach (var feed in feeds)
            {
                ndny += $"OR FeedId = \"{feed.Id}\" ";
            }
            ndny = ndny.Substring(3);

            Debug.WriteLine(ndny);

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedEntry WHERE " + ndny + (includeRead ? "" : $" AND Read = 0") + (includeHidden ? "" : $" AND Hidden = 0") + " ORDER BY Published " + (sortPublishedLatest ? "DESC" : "ASC")
            };
            var query = cmd.ExecuteReader();

            Debug.WriteLine(cmd.CommandText);

            while (query.Read())
            {
                feedEntries.Add(new FeedEntry()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1),
                    Summary = query.GetString(2),
                    Link = query.GetString(3),
                    Published = Utils.TimestampMsToDateTime(query.GetDouble(4)),
                    Received = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Read = query.GetBoolean(6),
                    Hidden = query.GetBoolean(7),
                    FeedId = query.GetString(8),
                });
            }

            return feedEntries;
        }

        public static List<FeedEntry> GetFeedEntries(Feed feed, bool includeRead = false, bool includeHidden = false, bool sortPublishedLatest = true)
        {
            List<FeedEntry> feedEntries = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedEntry WHERE FeedId = \"{feed.Id}\"" + (includeRead ? "" : $" AND Read = 0") + (includeHidden ? "" : $" AND Hidden = 0") + " ORDER BY Published " + (sortPublishedLatest ? "DESC" : "ASC")
            };
            var query = cmd.ExecuteReader();

            Debug.WriteLine(cmd.CommandText);

            while (query.Read())
            {
                feedEntries.Add(new FeedEntry()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1),
                    Summary = query.GetString(2),
                    Link = query.GetString(3),
                    Published = Utils.TimestampMsToDateTime(query.GetDouble(4)),
                    Received = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Read = query.GetBoolean(6),
                    Hidden = query.GetBoolean(7),
                    FeedId = query.GetString(8),
                });
            }

            return feedEntries;
        }

        public static List<FeedEntry> GetTodaysFeedEntries(bool includeRead = false, bool includeHidden = false, bool sortPublishedLatest = true)
        {
            List<FeedEntry> feedEntries = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedEntry WHERE Published >= {Utils.DateTimeToTimestamp(DateTime.Today, true)}" + (includeRead ? "" : $" AND Read = 0") + (includeHidden ? "" : $" AND Hidden = 0") + " ORDER BY Published " + (sortPublishedLatest ? "DESC" : "ASC")
            };
            var query = cmd.ExecuteReader();

            Debug.WriteLine(cmd.CommandText);

            while (query.Read())
            {
                feedEntries.Add(new FeedEntry()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1),
                    Summary = query.GetString(2),
                    Link = query.GetString(3),
                    Published = Utils.TimestampMsToDateTime(query.GetDouble(4)),
                    Received = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Read = query.GetBoolean(6),
                    Hidden = query.GetBoolean(7),
                    FeedId = query.GetString(8),
                });
            }

            return feedEntries;
        }

        public static List<FeedEntry> GetAllFeedEntries(bool includeRead = false, bool includeHidden = false, bool sortPublishedLatest = true)
        {
            List<FeedEntry> feedEntries = new();

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedEntry WHERE " + (includeRead ? "" : $"Read = 0") + (!includeRead & !includeHidden ? " AND " : "") + (includeHidden ? "" : $"Hidden = 0") + " ORDER BY Published " + (sortPublishedLatest ? "DESC" : "ASC")
            };
            var query = cmd.ExecuteReader();

            Debug.WriteLine(cmd.CommandText);

            while (query.Read())
            {
                feedEntries.Add(new FeedEntry()
                {
                    Id = query.GetString(0),
                    Title = query.GetString(1),
                    Summary = query.GetString(2),
                    Link = query.GetString(3),
                    Published = Utils.TimestampMsToDateTime(query.GetDouble(4)),
                    Received = Utils.TimestampMsToDateTime(query.GetDouble(5)),
                    Read = query.GetBoolean(6),
                    Hidden = query.GetBoolean(7),
                    FeedId = query.GetString(8),
                });
            }

            return feedEntries;
        }

        public static void UpdateFeedEntry(FeedEntry feedEntry)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedEntry SET Read = {Utils.BoolToIntStr(feedEntry.Read)}, Hidden = \"{Utils.BoolToIntStr(feedEntry.Hidden)}\" WHERE Id = \"{feedEntry.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }
    }

    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private readonly StorageFolder LocalFolder = ApplicationData.Current.LocalFolder;
        private StorageFile SettingsFile;

        private Settings mSettings;

        private bool SortPublishedLatest = true;
        private bool IncludeRead = false;
        private bool IncludeHidden = false;
        private string ViewStyle = "list";

        private readonly ObservableCollection<FeedEntry> FeedEntries = new();

        private FeedEntry CurrFeedEntry { get; set; }

        public MainPage()
        {
            InitializeComponent();

            Debug.WriteLine("Local folder path: " + LocalFolder.Path);

            DataAccess.InitDb();

            CurrFeedEntry = FeedEntry.Dummy();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            await LoadSettings();
            LoadMainNav();
            LoadFeed();
        }

        private async Task LoadSettings()
        {
            SettingsFile = await LocalFolder.CreateFileAsync("settings.json", CreationCollisionOption.OpenIfExists);

            string settingsJsonStr = await FileIO.ReadTextAsync(SettingsFile);
            JsonSchema settingsSchema = new JsonSchemaGenerator().Generate(typeof(Settings));

            try
            {
                JObject settingsJson = JObject.Parse(settingsJsonStr);

                if (settingsJson.IsValid(settingsSchema))
                {
                    mSettings = JsonConvert.DeserializeObject<Settings>(settingsJsonStr);
                }
                else
                {
                    mSettings = new Settings();
                    SaveSettings();
                }
            }
            catch (JsonReaderException)
            {
                mSettings = new Settings();
                SaveSettings();
            }
        }

        private async void SaveSettings()
        {
            await FileIO.WriteTextAsync(SettingsFile, JsonConvert.SerializeObject(mSettings));
        }

        private void LoadMainNav()
        {
            MainNavView.MenuItems.Clear();

            var todaysFeedItem = new NavigationViewItem() { Content = "Today", Tag = "TodayFeed", Icon = new Windows.UI.Xaml.Controls.SymbolIcon(Windows.UI.Xaml.Controls.Symbol.Calendar) };
            MainNavView.MenuItems.Add(todaysFeedItem);
            MainNavView.MenuItems.Add(new NavigationViewItemSeparator());
            MainNavView.MenuItems.Add(new NavigationViewItemHeader() { Content = "Feeds" });
            MainNavView.MenuItems.Add(new NavigationViewItem() { Content = "All", Tag = "AllFeed", Icon = new Windows.UI.Xaml.Controls.SymbolIcon(Windows.UI.Xaml.Controls.Symbol.List) });

            foreach (var feedFolder in DataAccess.GetFeedFolders())
            {
                NavigationViewItem feedFolderItem = new() { Content = feedFolder.Title, Tag = "Folder" + feedFolder.Id };

                foreach (var feed in DataAccess.GetFeeds(feedFolder))
                {
                    NavigationViewItem feedItem = new() { Content = feed.Title, Tag = feed.Id, Icon = new Windows.UI.Xaml.Controls.BitmapIcon() { UriSource = new Uri(feed.IconUrl), ShowAsMonochrome = false } };
                    feedFolderItem.MenuItems.Add(feedItem);
                }

                MainNavView.MenuItems.Add(feedFolderItem);
            }

            MainNavView.MenuItems.Add(new NavigationViewItem() { Content = "New Folder", Tag = "CreateNewFolder", Icon = new Windows.UI.Xaml.Controls.SymbolIcon(Windows.UI.Xaml.Controls.Symbol.NewFolder) });

            MainNavView.SelectedItem = todaysFeedItem;
        }

        private void LoadFeed()
        {
            FeedEntries.Clear();
            List<FeedEntry> newFeedEntries = new();

            var selectedFeedIdStr = (MainNavView.SelectedItem as NavigationViewItem).Tag.ToString();

            if (selectedFeedIdStr == "TodayFeed")
            {
                DataAccess.GetTodaysFeedEntries(IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = "Today's Feed";
                RenameBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;
            }
            else if (selectedFeedIdStr == "AllFeed")
            {
                DataAccess.GetAllFeedEntries(IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = "All Feed";
                RenameBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;
            }
            else if (selectedFeedIdStr.StartsWith("Folder"))
            {
                var selectedFeedFolderId = selectedFeedIdStr.Replace("Folder", "");
                var selectedFeedFolder = DataAccess.GetFeedFolder(selectedFeedFolderId);

                DataAccess.GetFeedEntries(selectedFeedFolder, IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = selectedFeedFolder.Title;
                RenameBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }
            else
            {
                var selectedFeedId = selectedFeedIdStr;
                var selectedFeed = DataAccess.GetFeed(selectedFeedId);

                DataAccess.GetFeedEntries(selectedFeed, IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = selectedFeed.Title;
                RenameBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
            }

        }

        private void RefreshFeed(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            FeedEntries.Clear();
            List<FeedEntry> newFeedEntries = new();

            var selectedFeedIdStr = (MainNavView.SelectedItem as NavigationViewItem).Tag.ToString();

            if (selectedFeedIdStr == "TodayFeed")
            {
                MainNavView.Header = "Today's Feed";
            }
            else if (selectedFeedIdStr == "AllFeed")
            {
                MainNavView.Header = "All Feed";
            }
            else if (selectedFeedIdStr.StartsWith("Folder"))
            {
                var selectedFeedFolderId = selectedFeedIdStr.Replace("Folder", "");
                var selectedFeedFolder = DataAccess.GetFeedFolder(selectedFeedIdStr);

                foreach (var feed in DataAccess.GetFeeds(selectedFeedFolder))
                {
                    SyndicationFeed feed_ = new();

                    using (WebClient client = new())
                    {
                        string feedStr = client.DownloadString(feed.Url);
                        feed_.Load(feedStr);
                    }

                    foreach (var entry in feed_.Items)
                    {
                        newFeedEntries.Add(FeedEntry.New(entry, feed.Id));
                    }
                }

                MainNavView.Header = selectedFeedFolder.Title;
            }
            else
            {
                var selectedFeedId = selectedFeedIdStr;
                var selectedFeed = DataAccess.GetFeed(selectedFeedId);

                SyndicationFeed feed_ = new();

                using (WebClient client = new())
                {
                    string feedStr = client.DownloadString(selectedFeed.Url);
                    feed_.Load(feedStr);
                }

                foreach (var entry in feed_.Items)
                {
                    newFeedEntries.Add(FeedEntry.New(entry, selectedFeedId));
                }

                DataAccess.AddFeedEntries(newFeedEntries, selectedFeed);

                MainNavView.Header = selectedFeed.Title;
            }

            if (SortPublishedLatest)
            {
                foreach (var newFeedEntry in newFeedEntries.OrderByDescending(x => x.Published.Ticks))
                {
                    FeedEntries.Add(newFeedEntry);
                }
            } else
            {
                foreach (var newFeedEntry in newFeedEntries.OrderBy(x => x.Published.Ticks))
                {
                    FeedEntries.Add(newFeedEntry);
                }
            }
        }

        private void FeedEntry_PointerEntered(object sender_, PointerRoutedEventArgs args)
        {
            var sender = (Windows.UI.Xaml.Controls.RelativePanel)sender_;

            var cmdBar = (Windows.UI.Xaml.Controls.StackPanel)sender.FindName("FeedEntryCmdBar");
            cmdBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }

        private void FeedEntry_PointerExited(object sender_, PointerRoutedEventArgs args)
        {
            var sender = (Windows.UI.Xaml.Controls.RelativePanel)sender_;

            var cmdBar = (Windows.UI.Xaml.Controls.StackPanel)sender.FindName("FeedEntryCmdBar");
            cmdBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private async void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem)
            {
                var selectedItem = args.SelectedItem as NavigationViewItem;

                switch (selectedItem.Tag.ToString())
                {
                    case "AddFeed":
                        var dialog = new AddFeedDialog(DataAccess.GetFeedFolders());
                        var result = await dialog.ShowAsync();

                        if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                        {
                            var newFeed = Feed.New(dialog.FeedUrl);

                            DataAccess.AddFeed(newFeed, DataAccess.GetFeedFolders()[dialog.GetSelectedFolderIndex()]);

                            LoadMainNav();
                        }

                        break;

                    case "CreateNewFolder":
                        var dialog1 = new CreateNewFolderDialog();
                        var result1 = await dialog1.ShowAsync();

                        if (result1 == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                        {
                            DataAccess.AddFeedFolder(FeedFolder.New(dialog1.FolderName));

                            LoadMainNav();
                        }

                        break;

                    case "Settings":
                        await new MessageDialog("Settings not implemented yet.", "Error").ShowAsync();
                        break;

                    case "Help":
                        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/ManbirJudge"));
                        break;

                    default:
                        LoadFeed();
                        break;
                }
            }
        }

        private void SortLatest(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SortPublishedLatest = true;
            LoadFeed();
        }

        private void SortOldest(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SortPublishedLatest = false;
            LoadFeed();
        }

        private void UpdateListViewStyle()
        {
            switch (ViewStyle)
            {
                case "List":
                    FeedEntriesListView.ItemTemplate = Resources["ItemStyleListTemplate"] as DataTemplate;
                    break;
                case "Magazine":
                    FeedEntriesListView.ItemTemplate = Resources["ItemStyleMagazineTemplate"] as DataTemplate;
                    break;
                case "Cards":
                    FeedEntriesListView.ItemTemplate = Resources["ItemStyleCardTemplate"] as DataTemplate;
                    break;
                case "Article":
                    FeedEntriesListView.ItemTemplate = Resources["ItemStyleArticleTemplate"] as DataTemplate;
                    break;
                default:
                    FeedEntriesListView.ItemTemplate = null;
                    break;
            }
        }

        private void OnUpdateListViewType(object sender_, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var sender = sender_ as RadioMenuFlyoutItem;
            ViewStyle = sender.Tag.ToString();

            UpdateListViewStyle();
        }

        private void FeedEntriesListView_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            if (FeedEntriesListView.SelectedIndex < 0)
            {
                MainSplitView.IsPaneOpen = false;
                MainContent.Padding = new Thickness(50, 10, 50, 10);

                return;
            }

            CurrFeedEntry = FeedEntries[FeedEntriesListView.SelectedIndex];

            CurrFeedEntry.Read = true;
            DataAccess.UpdateFeedEntry(CurrFeedEntry);

            PaneTitleTxt.Text = CurrFeedEntry.Title;
            PaneInfoPublishedTxt.Text = CurrFeedEntry.Published.ToString();
            PaneSummaryTxt.Text = CurrFeedEntry.Summary;

            MainSplitView.IsPaneOpen = true;
            MainContent.Padding = new Thickness(50, 10, 0, 10);
        }

        private async void RenameBtn_Click(object sender, RoutedEventArgs e)
        {
            RenameDialog dialog = new(MainNavView.Header.ToString());
            var result = await dialog.ShowAsync();

            if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                string selectedItemTag = (MainNavView.SelectedItem as NavigationViewItem).Tag.ToString();

                if (selectedItemTag.StartsWith("Folder"))
                {
                    selectedItemTag = selectedItemTag.Replace("Folder", "");

                    FeedFolder renamedFolder = DataAccess.GetFeedFolder(selectedItemTag);
                    renamedFolder.Title = dialog.NewTitle;
                    DataAccess.UpdateFeedFolder(renamedFolder);
                }
                else
                {
                    Feed renamedFeed = DataAccess.GetFeed(selectedItemTag);
                    renamedFeed.Title = dialog.NewTitle;
                    DataAccess.UpdateFeed(renamedFeed);
                }

                LoadMainNav();
            }
        }

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            void DeleteConfirmed(IUICommand command)
            {
                string selectedItemTag = (MainNavView.SelectedItem as NavigationViewItem).Tag.ToString();

                if (selectedItemTag.StartsWith("Folder"))
                {
                    selectedItemTag = selectedItemTag.Replace("Folder", "");

                    DataAccess.DeleteFeedFolder(selectedItemTag);
                }
                else
                {

                    DataAccess.DeleteFeed(selectedItemTag);
                }

                LoadMainNav();
            }

            void DeleteCanceled(IUICommand command)
            {
                Debug.WriteLine("Just a note. The user cancelled deletion.");
            }

            var dialog = new MessageDialog("Are you sure you want to delete the feed/feed folder?", "Confirmation");

            dialog.Commands.Add(new UICommand(
                "Yes",
                new UICommandInvokedHandler(DeleteConfirmed)
            ));
            dialog.Commands.Add(new UICommand(
                "Cancel",
                new UICommandInvokedHandler(DeleteCanceled)
            ));

            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 1;

            await dialog.ShowAsync();
        }

        private void ShowReadBtn_Click(object sender, RoutedEventArgs e)
        {
            IncludeRead = !IncludeRead;
            LoadFeed();
        }

        private void ShowHiddenBtn_Click(object sender, RoutedEventArgs e)
        {
            IncludeHidden = !IncludeHidden;
            LoadFeed();
        }
    }
}
