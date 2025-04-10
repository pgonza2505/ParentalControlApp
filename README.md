# 🛡️ Parental Control App for Windows

A lightweight C# WinForms application that monitors and restricts access to specific websites and applications on a Windows PC.

## ✨ Features

- 🔒 Password-protected access to settings
- 🔇 Blocks websites and apps based on title matches
- 📊 Logs blocked attempts with priority levels
- 🚨 Windows alert popups + system error sound
- 🪟 Minimizes to system tray and runs in background
- 🧠 Recovery system for forgotten admin passwords
- 🚫 Supports site priorities: low (log), mid (warn), high (kill browser)

## 📂 Project Structure

```
ParentalControlApp/
├── Program.cs
├── MainForm.cs
├── LoginForm.cs
├── ParentalControlApp.csproj
├── .gitignore
├── README.md
```

## 🔐 First-Time Setup
- Run the app → You'll be prompted to set an admin password
- A recovery code is generated for password reset (stored in `recovery.txt`)
- Add blocked site keywords + assign a priority

## 🚀 Build & Run
To build from terminal:

```bash
dotnet restore
dotnet build
dotnet run
```

## 📝 Release Notes

### v1.0.0 – Initial Release
- Core blocking and monitoring functionality
- Password and recovery system
- System tray integration
- Native Windows popups and sound feedback
- Full GitHub-ready structure

---

🔧 Built with ❤️ in C# for Windows environments.

Feel free to fork, modify, and contribute!
