using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;

namespace FeedReader
{
    public class Settings
    {
        string Theme { get; set; }
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
        public string ImgUrl { get; set; }

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
            newFeed.ImgUrl = feed.ImageUri.ToString();

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
                Id = item.Id ?? item.Links[0].Uri.ToString(),
                Title = item.Title.Text,
                Summary = item.Summary != null ? item.Summary.Text : "",
                Link = item.Links[0].Uri.ToString(),
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
                newFeedEntry.Links.Append(link.Uri.ToString());
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
                Summary = "Summary should be here.",
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

            cmd = "CREATE TABLE IF NOT EXISTS Feed (Id TEXT PRIMARY KEY, Url TEXT, Title TEXT, Link TEXT, Description TEXT, LastUpdated NUMERIC, Copyright TEXT, Language TEXT, IconUrl TEXT, ImgUrl TEXT, FolderId, FOREIGN KEY(FolderId) REFERENCES FeedFolder(Id) ON DELETE CASCADE);";
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
                CommandText = "INSERT INTO Feed VALUES ($Id, $Url, $Title, $Link, $Description, $LastUpdated, $Copyright, $Language, $IconUrl, $ImgUrl, $FolderId);"
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
            cmd.Parameters.AddWithValue("$ImgUrl", feed.ImgUrl);
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
                    ImgUrl = query.GetString(9),
                    FolderId = query.GetString(10)
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
                    ImgUrl = query.GetString(9),
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
                CommandText = $"UPDATE Feed SET Title = \"{feed.Title}\", Link = \"{feed.Link}\", Description = \"{feed.Description}\", LastUpdated = \"{Utils.DateTimeToTimestamp(feed.LastUpdated, true)}\", Language = \"{feed.Language}\", Copyright = \"{feed.Copyright}\", IconUrl = \"{feed.IconUrl}\", ImgUrl = \"{feed.ImgUrl}\" WHERE Id = \"{feed.Id}\";"
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

            // TODO: fix bug
            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = "INSERT INTO FeedEntry (Id, Title, Summary, Link, Published, Received, Read, Hidden, FeedId) VALUES ($Id, $Title, $Summary, $Link, $Published, $Received, $Read, $Hidden, $FeedId) ON CONFLICT (Id) DO UPDATE SET Title = EXCLUDED.Title, Summary = EXCLUDED.Summary, Link = EXCLUDED.Link, Published = EXCLUDED.Published, Received = EXCLUDED.Received, FeedId = EXCLUDED.FeedId;",
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

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"SELECT * FROM FeedEntry WHERE " + ndny + (includeRead ? "" : $" AND Read = 0") + (includeHidden ? "" : $" AND Hidden = 0") + " ORDER BY Published " + (sortPublishedLatest ? "DESC" : "ASC")
            };
            var query = cmd.ExecuteReader();

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

        public static void MarkFeedEntryAsRead(FeedEntry feedEntry)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedEntry SET Read = 1 WHERE Id = \"{feedEntry.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void MarkFeedEntryAsUnread(FeedEntry feedEntry)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedEntry SET Read = 0 WHERE Id = \"{feedEntry.Id}\";"
            };

            cmd.ExecuteNonQuery();
        }

        public static void MarkFeedEntriesAsReadBefore(DateTime datetime)
        {
            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedEntry SET Read = 1 WHERE Published < {Utils.DateTimeToTimestamp(datetime, true)};"
            };

            cmd.ExecuteNonQuery();
        }

        public static void HideFeedEntry(FeedEntry feedEntry)
        {
            if (!feedEntry.Read)
            {
                throw new Exception("The entry must be marked as read before hiding");
            }

            string dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, DbFileName);

            using SqliteConnection conn = new($"Filename={dbPath}");
            conn.Open();

            SqliteCommand cmd = new()
            {
                Connection = conn,
                CommandText = $"UPDATE FeedEntry SET Read = 1, Hidden = 1 WHERE Id = \"{feedEntry.Id}\";"
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

        private int SelectedFeedEntryI { get; set; } = 1;

        public MainPage()
        {
            InitializeComponent();

            Debug.WriteLine("Local folder path: " + LocalFolder.Path);

            DataAccess.InitDb();
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

            MainNavView.MenuItems.Add(new NavigationViewItem() { Content = "New Folder", Tag = "CreateNewFolder", SelectsOnInvoked = false, Icon = new Windows.UI.Xaml.Controls.SymbolIcon(Windows.UI.Xaml.Controls.Symbol.NewFolder) });

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
                RefreshFeedBtn.IsEnabled = false;
            }
            else if (selectedFeedIdStr == "AllFeed")
            {
                DataAccess.GetAllFeedEntries(IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = "All Feed";
                RenameBtn.IsEnabled = false;
                DeleteBtn.IsEnabled = false;
                RefreshFeedBtn.IsEnabled = true;
            }
            else if (selectedFeedIdStr.StartsWith("Folder"))
            {
                var selectedFeedFolderId = selectedFeedIdStr.Replace("Folder", "");
                var selectedFeedFolder = DataAccess.GetFeedFolder(selectedFeedFolderId);

                DataAccess.GetFeedEntries(selectedFeedFolder, IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = selectedFeedFolder.Title;
                RenameBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
                RefreshFeedBtn.IsEnabled = true;
            }
            else
            {
                var selectedFeedId = selectedFeedIdStr;
                var selectedFeed = DataAccess.GetFeed(selectedFeedId);

                DataAccess.GetFeedEntries(selectedFeed, IncludeRead, IncludeHidden, SortPublishedLatest).ForEach(FeedEntries.Add);

                MainNavView.Header = selectedFeed.Title;
                RenameBtn.IsEnabled = true;
                DeleteBtn.IsEnabled = true;
                RefreshFeedBtn.IsEnabled = true;
            }

        }

        private void RefreshFeed(object sender, RoutedEventArgs e)
        {
            var selectedIdStr = (MainNavView.SelectedItem as NavigationViewItem).Tag.ToString();

            if (selectedIdStr == "TodayFeed")
            {

            }
            else if (selectedIdStr == "AllFeed")
            {

            }
            else if (selectedIdStr.StartsWith("Folder"))
            {
                var selectedFeedFolderId = selectedIdStr.Replace("Folder", "");
                var selectedFeedFolder = DataAccess.GetFeedFolder(selectedFeedFolderId);

                foreach (var feed in DataAccess.GetFeeds(selectedFeedFolder))
                {
                    SyndicationFeed feed_ = new();
                    List<FeedEntry> thisFeedsEntries = new();

                    using (WebClient client = new())
                    {
                        string feedStr = client.DownloadString(feed.Url);
                        feed_.Load(feedStr);
                    }

                    foreach (var entry in feed_.Items)
                    {
                        thisFeedsEntries.Add(FeedEntry.New(entry, feed.Id));
                    }

                    DataAccess.AddFeedEntries(thisFeedsEntries, feed);
                }
            }
            else
            {
                var selectedFeedId = selectedIdStr;
                var selectedFeed = DataAccess.GetFeed(selectedFeedId);

                List<FeedEntry> thisFeedsEntries = new();

                SyndicationFeed feed_ = new();

                using (WebClient client = new())
                {
                    string feedStr = client.DownloadString(selectedFeed.Url);
                    feed_.Load(feedStr);
                }

                foreach (var entry in feed_.Items)
                {
                    thisFeedsEntries.Add(FeedEntry.New(entry, selectedFeedId));
                }

                DataAccess.AddFeedEntries(thisFeedsEntries, selectedFeed);
            }

            LoadFeed();
        }

        private void FeedEntry_PointerEntered(object sender_, PointerRoutedEventArgs args)
        {
            var sender = (Windows.UI.Xaml.Controls.RelativePanel)sender_;

            var cmdBar = (Windows.UI.Xaml.Controls.StackPanel)sender.FindName("FeedEntryCmdBar");
            cmdBar.Visibility = Visibility.Visible;
        }

        private void FeedEntry_PointerExited(object sender_, PointerRoutedEventArgs args)
        {
            var sender = (Windows.UI.Xaml.Controls.RelativePanel)sender_;

            var cmdBar = (Windows.UI.Xaml.Controls.StackPanel)sender.FindName("FeedEntryCmdBar");
            cmdBar.Visibility = Visibility.Collapsed;
        }

        private void MainNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem)
            {
                LoadFeed();
            }
        }

        private async void MainNavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var invokedItem = args.InvokedItemContainer as NavigationViewItem;

            if (invokedItem == null)
            {
                Debug.WriteLine("INVOEKD ITEM NULLLLLLLLLLLLLLLLLL");
                return;
            }

            var invokedItemTag = invokedItem.Tag.ToString();

            if (invokedItemTag == "AddFeed")
            {
                var dialog = new AddFeedDialog(DataAccess.GetFeedFolders());
                var result = await dialog.ShowAsync();

                if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    var newFeed = Feed.New(dialog.FeedUrl);

                    DataAccess.AddFeed(newFeed, DataAccess.GetFeedFolders()[dialog.GetSelectedFolderIndex()]);

                    LoadMainNav();
                }
            } else if(invokedItemTag == "CreateNewFolder")
            {
                var dialog = new CreateNewFolderDialog();
                var result = await dialog.ShowAsync();

                if (result == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
                {
                    DataAccess.AddFeedFolder(FeedFolder.New(dialog.FolderName));

                    LoadMainNav();
                }
            }
            else if (invokedItemTag == "Help")
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/ManbirJudge"));
            }
            else if (invokedItemTag == "Settings")
            {
                invokedItem.ContextFlyout.ShowAt(invokedItem);
            }
        }

        private void SortLatest(object sender, RoutedEventArgs e)
        {
            SortPublishedLatest = true;
            LoadFeed();
        }

        private void SortOldest(object sender, RoutedEventArgs e)
        {
            SortPublishedLatest = false;
            LoadFeed();
        }

        private void UpdateListViewStyle()
        {
            FeedEntriesListView.ItemTemplate = ViewStyle switch
            {
                "List" => Resources["ItemStyleListTemplate"] as DataTemplate,
                "Magazine" => Resources["ItemStyleMagazineTemplate"] as DataTemplate,
                "Cards" => Resources["ItemStyleCardTemplate"] as DataTemplate,
                "Article" => Resources["ItemStyleArticleTemplate"] as DataTemplate,
                _ => null,
            };
        }

        private void OnUpdateListViewType(object sender_, RoutedEventArgs e)
        {
            var sender = sender_ as RadioMenuFlyoutItem;
            ViewStyle = sender.Tag.ToString();

            UpdateListViewStyle();
        }

        private void SelectedFeedChanged()
        {
            if (SelectedFeedEntryI < 0)
            {
                MainSplitView.IsPaneOpen = false;
                MainContent.Padding = new Thickness(50, 10, 50, 10);

                return;
            }

            var notDecidedYet = FeedEntries[SelectedFeedEntryI];
            notDecidedYet.Read = true;
            DataAccess.UpdateFeedEntry(notDecidedYet);

            MainSplitView.IsPaneOpen = true;
            MainContent.Padding = new Thickness(50, 10, 0, 10);
        }

        private void FeedEntriesListView_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            SelectedFeedEntryI = FeedEntriesListView.SelectedIndex;
            PaneFlipView.SelectedIndex = SelectedFeedEntryI;

            SelectedFeedChanged();
        }

        private void PaneFlipView_SelectionChanged(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            SelectedFeedEntryI = PaneFlipView.SelectedIndex;
            FeedEntriesListView.SelectedIndex = SelectedFeedEntryI;

            SelectedFeedChanged();
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

        private void MarkAllAsReadBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAccess.MarkFeedEntriesAsReadBefore(DateTime.Now);
            LoadFeed();
        }

        private void MarkOlderThanDayAsReadBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAccess.MarkFeedEntriesAsReadBefore(DateTime.Today);
            LoadFeed();
        }

        private void MarkOlderThanWeekAsReadBtn_Click(object sender, RoutedEventArgs e)
        {
            DataAccess.MarkFeedEntriesAsReadBefore(Utils.StartOfWeek(DateTime.Now));
            LoadFeed();
        }

        private void ItemMarkAsReadBtn_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Button itemShareBtn = sender as Windows.UI.Xaml.Controls.Button;
            var item = itemShareBtn.DataContext as FeedEntry;

            if (!IncludeRead)
            {
                FeedEntries.Remove(item);
            }

            DataAccess.MarkFeedEntryAsRead(item);
        }

        private void ItemHideBtn_Click(object sender, RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Button itemShareBtn = sender as Windows.UI.Xaml.Controls.Button;
            var item = itemShareBtn.DataContext as FeedEntry;

            if (!IncludeHidden)
            {
                FeedEntries.Remove(item);
            };

            DataAccess.HideFeedEntry(item);
        }

        private async void PaneGoToWebBtn_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(FeedEntries[SelectedFeedEntryI].Link));
        }

        private void PaneInfoKeepUnreadBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DataAccess.MarkFeedEntryAsUnread(FeedEntries[SelectedFeedEntryI]);
            FeedEntries[SelectedFeedEntryI].Read = false;
        }

        private void PaneInfoHideBtn_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DataAccess.HideFeedEntry(FeedEntries[SelectedFeedEntryI]);
            FeedEntries.RemoveAt(SelectedFeedEntryI);
        }

        private async void ShareWhatsAppBtn_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"whatsapp://send?text={FeedEntries[SelectedFeedEntryI].Link}"));
        }

        private async void ShareTgBtn_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri($"tg://msg?text={FeedEntries[SelectedFeedEntryI].Link}"));
        }
    }
}
