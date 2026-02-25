# 🚀 Lost & Found App - Local Windows Setup Guide

Hello! Welcome to the local deployment guide for the **Lost & Found Application**.

This application has been designed specifically to run **100% locally on your own Windows hardware**. It does not require any cloud services, external databases, or third-party storage. Everything—from the database to the uploaded files—stays securely on your machine.

This guide will walk you through the entire process, from downloading the code off GitHub to getting the app running in **Visual Studio** on your Windows computer.

---

## 🛠️ Prerequisites Guide

Before you begin, please ensure your Windows machine has the following software installed:

1. **Visual Studio 2022** (Community, Professional, or Enterprise)
   * *Important:* When installing, ensure the **"ASP.NET and web development"** workload is checked.
2. **.NET 8.0 SDK**
   * This is usually installed automatically with recent versions of Visual Studio. 
3. **A Local MySQL Server**
   * You must have MySQL running on your computer. You can use **XAMPP**, **MySQL Workbench**, or the standalone **MySQL Community Server**.
   * *Note: Please ensure the MySQL service is actively running before proceeding.*

---

## 📥 Step 1: Download & Extract the Project

If you are downloading the source code directly from GitHub as a ZIP file, please follow these steps carefully to assure Windows doesn't block the files:

1. On the GitHub repository page, click the green **"<> Code"** button.
2. Select **"Download ZIP"** from the dropdown menu and save the file to your computer (e.g., your `Downloads` folder).
3. **⚠️ Windows Security Step:** 
   * Navigate to where you downloaded the `.zip` file.
   * Right-click the `.zip` file and select **Properties**.
   * Look at the bottom of the General tab. If you see a security warning saying *"This file came from another computer and might be blocked..."*, check the **Unblock** box and click **Apply**, then **OK**.
4. Right-click the `.zip` file and select **Extract All...**
5. Choose a destination folder where you want your project to live (for example: `C:\Projects\LostAndFound`).

---

## 💻 Step 2: Open the Project in Visual Studio

1. Open your File Explorer and navigate to the extracted folder (e.g., `C:\Projects\LostAndFound`).
2. Inside, locate the **`LostAndFoundApp.csproj`** file (or the `.sln` file if present).
3. Double-click the file to open it. **Visual Studio 2022** will automatically launch.
4. *Wait a moment:* Visual Studio will spend a few seconds automatically downloading and restoring the required NuGet code packages. You can watch the progress in the "Output" window at the bottom of the screen.

---

## 🗄️ Step 3: Configure Your MySQL Connection

You need to tell the application how to talk to your local MySQL server.

1. On the right side of Visual Studio, look at the **Solution Explorer** panel.
2. Find and double-click the **`appsettings.json`** file to open it.
3. At the very top of the file, you will see the `"ConnectionStrings"` section:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Port=3306;Database=LostAndFoundDb;User=root;Password=your_password_here;"
   }
   ```
4. **Action Required:** Update the `User` and `Password` to match your local MySQL Server credentials.
   * *Tip:* If you are using a default local install like XAMPP, the username is usually `root` and the password is left entirely blank. If so, it should look exactly like this: `User=root;Password=;`

---

## ▶️ Step 4: Run the Application

You are now ready to start the app! Let's hit play.

1. Look at the top-middle toolbar in Visual Studio.
2. You will see a green **"Play"** button (Start Debugging). 
   * *Important:* Click the small dropdown arrow next to the Play button and ensure either **"http"**, **"https"**, or **"LostAndFoundApp"** is selected. (Try to avoid selecting "IIS Express", as the standard profiles provide better console logging).
3. Click the **Green Play Button** (or press **F5** on your keyboard).

---

## 🪄 Step 5: System Automation (The Magic)

As soon as you click Play, a black console window will open. Here is what the application does automatically for you on its very first run:

* **Creates the Database:** It talks to your MySQL Server and creates a brand-new database called `LostAndFoundDb`.
* **Builds the Tables:** It automatically generates all the required SQL tables (Users, Items, Categories, etc.) without you having to run any manual SQL scripts.
* **Seeds Default Data:** Because the setting `SEED_DATABASE=true` is enabled behind the scenes, the app instantly populates the database with default test data and the SuperAdmin account so the app is immediately usable.

---

## 🎉 Step 6: Accessing the App

1. Visual Studio will automatically launch your default web browser.
2. You will be directed to the application's local address (usually something like `https://localhost:5001`).
3. You will be greeted by the Lost & Found login screen! 
4. **Where do my files go?** When you upload images or attachments within the app, they will automatically be saved to a folder named `SecureStorage` that the app creates directly inside your project directory. 

---

### 🛑 Troubleshooting Quick-Check
* **The app crashes instantly when I press Play:** Double-check that your MySQL server (via the XAMPP Control Panel or Windows Services) is actually turned **ON** and running.
* **Database Connection Error:** Verify that the username and password you typed into Step 3 (`appsettings.json`) are perfectly correct.
