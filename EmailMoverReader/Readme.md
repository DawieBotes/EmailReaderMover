# EmailMoverReader

## Overview
**EmailMoverReader** is a C# command-line tool that connects to an IMAP email server, reads the first email from a specified folder, and optionally moves it to another folder. The program outputs the email details as a JSON response.

## Features
- Connects to an IMAP server.
- Reads the first available email from a specified folder.
- Returns email details in JSON format.
- Optionally moves the email to another folder.
- Includes attachment data in Base64 format.

## Requirements
- .NET 6 or later
- IMAP-enabled email account

## Setup & Build
### Prerequisites
1. Install [.NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet)
2. Clone or download the repository.

### Build Instructions
1. Open a terminal in the project folder.
2. Run the following command to build the project:
   ```sh
   dotnet build
   ```
3. The compiled executable will be available in the `bin/Debug/net6.0/` or `bin/Release/net6.0/` directory.

### Running the Program
You can execute the program using `dotnet run` or directly run the compiled `.exe` file.

#### Example Usage:
```sh
EmailMoverReader.exe --server imap.example.com --username user@example.com --password secret --port 993 --readfolder INBOX --movetofolder ProcessedEmails
```

#### Required Parameters:
- `--server` : IMAP server address
- `--username` : Email account username
- `--password` : Email account password
- `--port` : IMAP server port (usually 993 for SSL)
- `--readfolder` : Folder to read emails from

#### Optional Parameters:
- `--movetofolder` : Folder to move the email to (if not specified, the email remains in the original folder)

### Example JSON Output
```json
{
  "Success": true,
  "Message": "Email fetched successfully.",
  "Email": {
    "Subject": "Test Email",
    "From": "sender@example.com",
    "To": "receiver@example.com",
    "Body": "Hello, this is a test email!",
    "ReceivedDate": "2025-02-26T12:34:56Z",
    "Attachments": [],
    "UniqueId": "123"
  }
}
```

### Error Handling
- If required parameters are missing, a JSON response with `Success: false` and an error message is returned.
- If the specified folder does not exist, an error message with available folders is returned.
- If an error occurs during execution, an appropriate error message is displayed.

## License
This project is licensed under the MIT License.

## Contact
For any issues or suggestions, feel free to reach out!
