
using System;
using System.IO;

namespace AudioRecorderApp {
  class Program {
    static void Main(string[] args) {
      string currentDir = Directory.GetCurrentDirectory();
      string serverBaseFolder = Path.Combine(currentDir, "html", "dist");
      string databasePath = Path.Combine(currentDir, "AudioPrograms.db");
      Console.WriteLine($"Current DB Path: {databasePath}");
      
      
      Handlers handlers = new Handlers(serverBaseFolder,databasePath);
      handlers.StartServer();
    }
  }
}
