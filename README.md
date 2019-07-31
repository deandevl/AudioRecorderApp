AudioRecorder
=============

**AudioRecorder** provides a way to schedule at specific times and record (with .mp3 format) future live internet audio streams. The application uses a locally run C# web server which requires that  Microsoft .NET Framework (>= 4.7) is installed on the client machine. **AudioRecorder** has an embedded backend database  [LiteDB](https://github.com/mbdavid/LiteDB)  for storing program urls, start/stop times and days of the week. The database located in the root of the install folder (i.e. `AudioPrograms.db`) is preloaded with internet streaming programs from National Public Radio.  Make sure that the user has the `Full Control` security  level for the database file.  The [AudioRecorder repository](https://github.com/deandevl/AudioRecorder) contains both the source code and a Windows 10 `setup.exe` installer.  A help tab is provided on the main page describes how to use **AudioRecorder** from a Chrome browser.

There are desktop and start menu shortcuts provided to start the local server (i.e.`AudioRecorderApp.exe`) server.  After starting the server, enter `localhost:8081` in a Chrome browser's address box to show **AudioRecorder** start page.

Note that if the `Cancel Recordings` button is clicked, then you will need to restart the server.

