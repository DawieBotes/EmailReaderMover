using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System.Linq;
using System.IO;

class Program
{
    static void Main(string[] args)
{
    var parameters = new Dictionary<string, string>();

    for (int i = 0; i < args.Length; i++)
    {
        if (args[i].StartsWith("--"))
        {
            var paramName = args[i].Substring(2);
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
            {
                parameters[paramName] = args[i + 1];
                i++;
            }
            else
            {
                Console.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = $"Missing value for parameter: {args[i]}", Email = new { } }, Formatting.Indented));
                return;
            }
        }
        else
        {
            Console.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = $"Invalid argument format: {args[i]}", Email = new { } }, Formatting.Indented));
            return;
        }
    }

    var requiredParams = new string[] { "server", "username", "password", "port", "readfolder" };
    var missingParams = requiredParams.Where(param => !parameters.ContainsKey(param)).ToList();

    if (missingParams.Count > 0)
    {
        Console.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = $"Missing parameters: {string.Join(", ", missingParams)}", Email = new { } }, Formatting.Indented));
        return;
    }

    string server = parameters["server"];
    string username = parameters["username"];
    string password = parameters["password"];
    int port = int.Parse(parameters["port"]);
    string readFolder = parameters["readfolder"];
    string moveToFolder = parameters.ContainsKey("movetofolder") ? parameters["movetofolder"] : "";

    UniqueId emailUniqueId;
    var email = ConnectToImapServer(server, username, password, port, readFolder, out emailUniqueId);

    if (email == null)
    {
        Console.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = "Failed to retrieve email.", Email = new { } }, Formatting.Indented));
        return;
    }

    var response = new { Success = true, Message = "Email fetched successfully.", Email = email };

    if (!string.IsNullOrEmpty(moveToFolder) && emailUniqueId != UniqueId.Invalid)
    {
        using (var client = new ImapClient())
        {
            client.Connect(server, port, true);
            client.Authenticate(username, password);

            var moveResult = MoveEmailToFolder(client, emailUniqueId, moveToFolder);
            client.Disconnect(true);

            // Update response to include move status
            response = new { Success = moveResult.Success, Message = moveResult.Message, Email = email };
        }
    }

    Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
}


    static EmailData ConnectToImapServer(string server, string username, string password, int port, string readFolder, out UniqueId emailUniqueId)
    {
        emailUniqueId = UniqueId.Invalid;
        try
        {
            using (var client = new ImapClient())
            {
                client.Connect(server, port, true);
                client.Authenticate(username, password);

                var folder = client.GetFolder(readFolder);
                folder.Open(FolderAccess.ReadWrite);

                var messages = folder.Fetch(0, -1, MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope);
                var messageSummary = messages.FirstOrDefault();

                if (messageSummary == null) return null;

                emailUniqueId = messageSummary.UniqueId;
                var mimeMessage = folder.GetMessage(emailUniqueId);
                var attachments = GetAttachments(mimeMessage);

                client.Disconnect(true);

                return new EmailData(
                    mimeMessage.Subject,
                    mimeMessage.From.ToString(),
                    mimeMessage.To.ToString(),
                    mimeMessage.TextBody,
                    mimeMessage.Date.UtcDateTime,
                    attachments,
                    emailUniqueId.ToString()
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    static (bool Success, string Message) MoveEmailToFolder(ImapClient client, UniqueId emailUniqueId, string moveToFolder)
{
    try
    {
        // Get the root folder and list all available folders
        var personalNamespace = client.GetFolder(client.PersonalNamespaces[0]);
        var availableFolders = personalNamespace.GetSubfolders().Select(f => f.FullName).ToList();

        // Check if the target folder exists
        if (!availableFolders.Contains(moveToFolder))
        {
            return (false, $"The folder '{moveToFolder}' does not exist. Available folders: {string.Join(", ", availableFolders)}");
        }

        // Get the destination folder
        var destinationFolder = client.GetFolder(moveToFolder);
        destinationFolder.Open(FolderAccess.ReadWrite);

        // Move the email
        var inbox = client.Inbox;
        inbox.Open(FolderAccess.ReadWrite);
        inbox.MoveTo(emailUniqueId, destinationFolder);

        return (true, $"Email moved to {moveToFolder} successfully.");
    }
    catch (Exception ex)
    {
        return (false, $"Error moving email: {ex.Message}");
    }
}



    static List<AttachmentData> GetAttachments(MimeMessage message)
    {
        var attachments = new List<AttachmentData>();
        foreach (var attachment in message.Attachments)
        {
            if (attachment is MimePart mimePart)
            {
                using (var memoryStream = new MemoryStream())
                {
                    mimePart.Content.DecodeTo(memoryStream);
                    attachments.Add(new AttachmentData { FileName = mimePart.FileName, ContentType = mimePart.ContentType.MimeType, Base64Content = Convert.ToBase64String(memoryStream.ToArray()) });
                }
            }
        }
        return attachments;
    }
}

public class EmailData
{
    public string Subject { get; set; }
    public string From { get; set; }
    public string To { get; set; }
    public string Body { get; set; }
    public DateTime ReceivedDate { get; set; }
    public List<AttachmentData> Attachments { get; set; }
    public string UniqueId { get; set; }

    public EmailData(string subject, string from, string to, string body, DateTime receivedDate, List<AttachmentData> attachments, string uniqueId)
    {
        Subject = subject;
        From = from;
        To = to;
        Body = body;
        ReceivedDate = receivedDate;
        Attachments = attachments ?? new List<AttachmentData>();
        UniqueId = uniqueId;
    }
}

public class AttachmentData
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public string Base64Content { get; set; }
}
