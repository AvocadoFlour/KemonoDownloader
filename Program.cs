
using HtmlAgilityPack;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;

const string pagination = "?o=";
const string kemonoBaseUrl = "https://kemono.party/";

Console.WriteLine("Input (paste or manually type in) the artist URL. The program will just shut off if you input something invalid. Already existing files will not be downloaded anew.");
var artistUrl = Console.ReadLine();
Console.WriteLine("Do you want all of the media from a single post to also be put into post-based folder? \n");

//https://stackoverflow.com/a/41609801/10299831
string choice;
do
{
    Console.WriteLine("Input \"N\" for: artistname\\artworks file hierarcy. Input \"Y\" for: artistname\\post\\artworks file hierarcy.");
    choice = Console.ReadLine();
    var choiceLower = choice?.ToLower();
    if ((choiceLower == "y") || (choiceLower == "n"))
        break;
} while (true);


HtmlWeb hw = new HtmlWeb();

var artistIndex = TryLoop(() =>
    {
        return GetAllPagesAndArtistName(artistUrl);
    }
);

var fullPages = artistIndex.Item1 / 25;

if(artistIndex.Item1% 25 == 0)
{
    fullPages -= 1;
}

System.IO.Directory.CreateDirectory(artistIndex.Item2);

for (int i = 0; i <= fullPages; i++)
{
    var postIds = TryLoop(() =>
    {
        return GetAllPostOnAPage(artistUrl + pagination + i * 25);
    });

    foreach (string postId in postIds)
    {
        Sleep(1);
        
        GetPostAttachments(artistUrl + "/post/" + postId);
        GetImagesFromASinglePost(artistUrl + "/post/" + postId);
    }
}

Tuple<int, string> GetAllPagesAndArtistName(string artistUrl)
{
    HtmlDocument doc = new HtmlDocument();
    doc = hw.Load(artistUrl);
    var paginator = doc.DocumentNode.SelectSingleNode("//small");
    int pages = int.Parse(paginator.InnerHtml.Split("\n")[1].Split(" ").Last());
    var artistNameator = doc.DocumentNode.SelectSingleNode("//*[@itemprop='name']");
    string artistName = artistNameator.GetDirectInnerText();
    return Tuple.Create(pages, artistName);
}


// get all posts of an artist on one page
List<string> GetAllPostOnAPage(string pageUrl)
{
    List<string> postIds = new List<string>();
    var doc = hw.Load(pageUrl);
    foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//article[contains(@class, 'post-card')]"))
    {
        // Get the values of the href attribute of each post on the page
        string postId = div.GetAttributeValue("data-id", string.Empty);
        postIds.Add(postId);
    }
    return postIds;
}

void GetImagesFromASinglePost(string postUrl)
{
    HtmlDocument doc = new HtmlDocument();
    doc = TryLoop(() =>
    {
        return hw.Load(postUrl);
    });

    if (doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]") != null)
    {
        foreach (HtmlNode div in doc.DocumentNode.SelectNodes("//div[contains(@class, 'post__files')]"))
        {
            var counter = 0;
            // Get the value of the HREF attribute
            foreach (HtmlNode url in div.SelectNodes("//a[contains(@class, 'fileThumb')]"))
            {
                string hrefValue = url.GetAttributeValue("href", string.Empty);
                string extension = hrefValue.Split(".").Last();
                if (extension.Equals("jpe"))
                {
                    extension = "jpg";
                }
                var fileName = ValidateFileName(postUrl.Split("post/")[1] + "_" + counter + "." + extension);

                string postFolder = artistIndex.Item2;

                // create a folder for each post as well
                if (choice.Equals("y")) {
                    var postName = GetPostName(doc);
                    postFolder = postFolder + "\\" + postName;
                }

                // Make sure that the directory exists
                System.IO.Directory.CreateDirectory(postFolder);


                if (!CheckIfFileExists(postFolder + "\\" + fileName))
                {
                    Console.WriteLine($"Saving: {fileName}");
                    if (extension.Equals("gif"))
                    {
                        SaveGif(kemonoBaseUrl + hrefValue, postFolder + "\\" + fileName);
                    }
                    else SaveImage(kemonoBaseUrl + hrefValue, postFolder + "\\" + fileName);

                    Sleep();
                }
                else Console.WriteLine($"File exists, skipping: {fileName}");
                counter += 1;
            }
        }
    }
}

void GetPostAttachments(string postUrl)
{
    var doc = TryLoop(() =>
    {
        return hw.Load(postUrl);
    });

    if (doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]") != null)
    {
        foreach (HtmlNode attachment in doc.DocumentNode.SelectNodes("//a[contains(@class, 'post__attachment-link')]"))
        {
            var url = attachment.GetAttributeValue("href", string.Empty);
            var fileName = attachment.InnerText;
            fileName = ValidateFileName(postUrl.Split("post/")[1] + "_" + fileName.Split("\n")[1].TrimStart().Split("\n")[0]);
            if (!CheckIfFileExists(artistIndex.Item2 + "\\" + fileName))
            {
                var fullUrl = kemonoBaseUrl + url;
                Console.WriteLine("Downloading: " + fullUrl);
                WebClient webClient = new WebClient();
                Console.WriteLine($"Downloading attachment: {fileName}");

                // Create post folder
                if (choice.Equals("y"))
                {
                    System.IO.Directory.CreateDirectory(artistIndex.Item2);
                }

                TryLoopAction(() =>
                {
                    webClient.DownloadFile(new Uri(fullUrl), artistIndex.Item2 + "\\" + fileName);
                });
                Console.WriteLine("Download done.");
                webClient.Dispose();
                Sleep();
            }
            else Console.WriteLine($"File exists, skipping: {fileName}");
        }
    }
}

void SaveImage(string imageUrl, string filePath)
{
    WebClient client = new WebClient();

    Console.WriteLine("Downloading: " + imageUrl);
    Stream stream = TryLoop(() =>
    {
        return client.OpenRead(imageUrl);
    });
    Bitmap bitmap = null;
    TryLoopAction(() =>
    {
        bitmap = new Bitmap(stream);
    });

    if (bitmap != null)
    {
        bitmap.Save(filePath);
    }

    stream.Flush();
    stream.Close();
    client.Dispose();
}

void SaveGif(string gifUrl, string filePath)
{
    Console.WriteLine("Downloading: " + gifUrl);
    WebClient webClient = new WebClient();
    Console.WriteLine($"Downloading attachment: {filePath}");
    TryLoopAction(() =>
    {
        webClient.DownloadFile(new Uri(gifUrl), filePath);
    });
    webClient.Dispose();
}

bool CheckIfFileExists(string fileName)
{
    var workingDirectory = Environment.CurrentDirectory;
    var file = $"{workingDirectory}\\{fileName}";
    return File.Exists(file);
}

void Sleep(int length = 1)
{
    Random rnd = new Random();
    var randInt = 0;
    if (length == 0)
    {
        randInt = rnd.Next(1354, 5987);
        Console.WriteLine($"Next post, slept for {randInt} miliseconds so as not to overburden the site.");
    }
    else
    {
        randInt = rnd.Next(585, 3576);
        Console.WriteLine($"Slept for {randInt} miliseconds so as not to overburden the site.");
    }

    Thread.Sleep(randInt);
}

string ValidateFileName(string input, string replacement = "")
{
    var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
    var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
    var fileName = r.Replace(input, replacement);
    if (fileName.Length > 120)
    {
        fileName = fileName.Substring(0, 119);
    }
    return fileName;
}

// https://stackoverflow.com/a/23103561/10299831
T TryLoop<T>(Func<T> anyMethod)
{
    while (true)
    {
        try
        {
            return anyMethod();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            System.Threading.Thread.Sleep(2000); // *
        }
    }
    return default(T);
}

void TryLoopAction(Action anyAction)
{
    while (true)
    {
        try
        {
            anyAction();
            break;
        }
        catch(Exception e)
        {
            Console.WriteLine(e.Message);
            System.Threading.Thread.Sleep(2000); // *
        }
    }
}

string GetPostName(HtmlDocument doc)
{
    return doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'post__title')]").ChildNodes.ElementAt(1).InnerText;
}