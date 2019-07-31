using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using BasicWebServerLib;
using BasicWebServerLib.Events;
using BasicWebServerLib.HttpCommon;
using BasicWebServerLib.WsCommon;
using LiteDB;
using AudioRecorderApp.database;

namespace AudioRecorderApp {
  public class Handlers {
    private readonly string _serverBaseFolder;
    private readonly Helpers _helpers;
    private readonly Dictionary<string, Action> _actions;
    private Dictionary<string, object> _requestDictionary;
    private HttpConnectionDetails _httpDetails;
    private readonly LiteDatabase _db;
    private LiteCollection<AudioProgram> _programsCollection;
    private LiteCollection<AudioGroup> _groupsCollection;
    private LiteCollection<AudioSession> _sessionCollection;
    private List<Dictionary<string,object>> _programsToRecord;
    private WsFrameWriter _wsFrameWriter;
    private DateTime _startTime;
    private double _delay;
    private double _duration;
    private int _currentRecordingIdx;
    private bool _recordingFinished;
    private bool _processingCancelled;
    private IObservable<long> _timer;
    
    public Handlers(string serverBaseFolder, string databasePath) {
      _serverBaseFolder = serverBaseFolder;
      _helpers = new Helpers();
      JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
      
      _db = new LiteDatabase(databasePath);
      _programsCollection = _db.GetCollection<AudioProgram>("ProgramsCollection");
      _groupsCollection = _db.GetCollection<AudioGroup>("GroupsCollection");
      _sessionCollection = _db.GetCollection<AudioSession>("SessionCollection");
      
      //create database if necessary
      if(!File.Exists(databasePath)) {
        Dictionary<string, object> program1 = new Dictionary<string, object>() {
          {"Name", "prog_1"},
          {"Start", 654},
          {"Stop",655},
          {"Dow",new ArrayList(){0,1,2,3,4,5,6}},
          {"Url",@"https://dal-wku-stream-1.neighborhoodca.com/stream"},
          {"GroupName","BG Test"}
        };
        AddProgram(program1);
        Dictionary<string, object> program2 = new Dictionary<string, object>() {
          {"Name", "prog_2"},
          {"Start", 654},
          {"Stop",655},
          {"Dow",new ArrayList(){0,1,2,3,4,5,6}},
          {"Url",@"http://lpm.streamguys1.com/wfpl-popup"},
          {"GroupName","Louisville Test"}
        };
        AddProgram(program2);

        AddSession("BG Test");
      };

      _actions = new Dictionary<string, Action>() {
        {"getGroupNames", () => {
          List<string> groupNames = GetGroupNames();
          string responseStr = jsonSerializer.Serialize(groupNames);
          _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
        }},
        {"getSession", () => {
          string groupName = GetSession();
          _helpers.SendHttpTextResponse(_httpDetails.Response,groupName);
        }},
        {"setSession", () => {
          string groupName = (string)_requestDictionary["GroupName"];
          bool found = UpdateSession(groupName);
          if(found) {
            _helpers.SendHttpTextResponse(_httpDetails.Response, "Successfully updated session.");
          } else {
            _helpers.SendHttpResponse(400, "Could not locate group",new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"getPrograms", () => {
          string groupName = (string)_requestDictionary["GroupName"];
          List<Dictionary<string, object>> programsList = GetPrograms(groupName);
          string responseStr = jsonSerializer.Serialize(programsList);
          _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
        }},
        {"addProgram", () => {
          Dictionary<string,object> programDict = _helpers.JsonToDictionary(_requestDictionary["program"].ToString());
          AudioProgram newProgram = AddProgram(programDict);
          if(newProgram != null) {
            //return both a list of group names and the new program
            List<string> groupNames = GetGroupNames();
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"backup_program", newProgram.Program()},
              {"group_names", groupNames}
            };
            string responseStr = _helpers.DictionaryToJson(responseDict);
            _helpers.SendHttpTextResponse(_httpDetails.Response,responseStr);
          } else {
            _helpers.SendHttpResponse(400, "Duplicate Program Found",new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"deleteProgram", () => {
          Dictionary<string,object> programDict = _helpers.JsonToDictionary(_requestDictionary["program"].ToString());
          string groupName = (string)programDict["GroupName"];
          string programName = (string)programDict["Name"];
          AudioProgram program = _programsCollection.FindOne(x => x.GroupName == groupName && x.Name == programName);
          if(program != null) {
            DeleteProgram(program.Id, program.GroupId);
            //return the current set of group names and backup program
            List<string> groupNames = GetGroupNames();
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"backup_program", program.Program()},
              {"group_names", groupNames}
            };

            string responseStr = _helpers.DictionaryToJson(responseDict);
            _helpers.SendHttpTextResponse(_httpDetails.Response, responseStr);
          } else {
            _helpers.SendHttpResponse(400, "Could not locate program",new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }},
        {"updateProgram", () => {
          Dictionary<string,object> programDict = _helpers.JsonToDictionary(_requestDictionary["program"].ToString());
          AudioProgram backupProgram = UpdateProgram(programDict);
          if(backupProgram != null) {
            //return the current set of group names
            List<string> groupNames = GetGroupNames();
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"backup_program", backupProgram.Program()},
              {"group_names", groupNames}
            };
            string responseStr = _helpers.DictionaryToJson(responseDict);
            _helpers.SendHttpTextResponse(_httpDetails.Response, responseStr);
          } else {
            _helpers.SendHttpResponse(400, "Could not locate program. Try adding instead of updating ",new byte[0],"text/html","MoneyTracker Server", _httpDetails.Response);
          }
        }}
      };
    }

    public void StartServer() {
      BasicWebServer basicServer = new BasicWebServer(baseFolderPath: _serverBaseFolder,tcpPort:54002,httpPrefix:"http://localhost:8081/");
      basicServer.WsTextFrameChanged += WsTextFrameChanged;
      basicServer.HttpRequestChanged += HttpRequestChanged;
      
      basicServer.Start();
    }
    
    public void WsTextFrameChanged(object sender, EventArgs args) {
      WsTextFrameEventArgs wsArgs = (WsTextFrameEventArgs)args;
      WsConnectionDetails details = wsArgs.Details;
      string message = (string)wsArgs.Message;
      string pathOrFileName = details.PathOrFileName;  //debug
      NetworkStream stream = details.Stream;
      Dictionary<string, object> dict = _helpers.JsonToDictionary(message);

      string action = (string)dict["action"];
      if(action.Equals("startRecording")) {
        _wsFrameWriter = new WsFrameWriter(stream);
        string groupName = (string)dict["GroupName"];
        ProcessRecordings(groupName);
      }else if(action.Equals("cancelProcessing")) {
        _processingCancelled = true;
        _recordingFinished = true;
        Console.WriteLine("Processing Cancelled");
        Dictionary<string,object> responseDict = new Dictionary<string, object>() {
          {"action", "cancelProcessing"},
          {"message", "Processing Cancelled"}
        };
        _helpers.SendWsText(_wsFrameWriter, responseDict);
      }
    }

    public void HttpRequestChanged(object sender, EventArgs args) {
      HttpRequestEventArgs httpArgs = (HttpRequestEventArgs)args;
      _httpDetails = httpArgs.Details;
      string body = (string)httpArgs.Body;
      
      if(_httpDetails.HttpPath == "audio") {
        _requestDictionary = _helpers.JsonToDictionary(body);
        _actions[(string)_requestDictionary["action"]]();
      }
    }
    
    private AudioProgram AddProgram(Dictionary<string,object> programDict) {
      string groupName = (string)programDict["GroupName"];
      string programName = (string)programDict["Name"];
      
      //does the group exist in db?
      int groupId;
      AudioGroup group = _groupsCollection.FindOne(x => x.Name.Equals(groupName));
      if(group == null) { //if not then create it
        groupId = AddGroup(groupName);
      } else {
        groupId = group.Id;
      }
      
      //does the program exist with this group
      AudioProgram program = _programsCollection.FindOne(x => x.GroupId == groupId && x.Name.Equals(programName));
      if(program == null) {
        AudioProgram newProgram = new AudioProgram();
        newProgram.Name = (string)programDict["Name"];
        newProgram.Start = Convert.ToInt32(programDict["Start"]);
        newProgram.Stop = Convert.ToInt32(programDict["Stop"]);
        newProgram.Dow = _helpers.JArrayToArrayList(programDict["Dow"]);
        newProgram.Url = (string)programDict["Url"];
        newProgram.GroupId = groupId;
        newProgram.GroupName = groupName;

        _programsCollection.Insert(newProgram);
        return newProgram;
      } else {
        return null;
      }
    }

    private int DeleteProgram(int programId,int groupId) {
      int deletedCount = _programsCollection.Delete(x => x.Id == programId);
      //is this the last program for program's group?
      IEnumerable<AudioProgram> programs = _programsCollection.Find(x => x.GroupId.Equals(groupId));
      if(programs.Count() == 0) {
        //there are no programs with groupId, so delete the group
        _groupsCollection.Delete(x => x.Id.Equals(groupId));
      }
      return deletedCount;
    }

    private AudioProgram UpdateProgram(Dictionary<string, object> programDict) {
      string currentGroupName = (string)programDict["currentGroupName"];
      string updateGroupName = (string)programDict["updateGroupName"];
      string currentProgramName = (string)programDict["currentName"];
      string updateProgramName = (string)programDict["updateName"];
      
      AudioProgram currentProgram = _programsCollection.FindOne(x => x.GroupName == currentGroupName && x.Name == currentProgramName);
      if(currentProgram != null) {
        //are we changing the group name
        if(!updateGroupName.Equals(currentProgram.GroupName)) {
          //we are changing the group name; so is the current program the last program in the current group?
          //if so then delete the old group
          IEnumerable<AudioProgram> programs = _programsCollection.Find(x => x.GroupName.Equals(currentProgram.GroupName));
          if(programs.Count() == 1) {
            //this is  the last program with a GroupName that is changing, so delete the group
            _groupsCollection.Delete(x => x.Name.Equals(currentProgram.GroupName));
          }
        }
        //does the new group exist in db?
        int groupId;
        AudioGroup group = _groupsCollection.FindOne(x => x.Name.Equals(updateGroupName));
        if(group == null) { //if not then create it
          groupId = AddGroup(updateGroupName);
        } else {
          groupId = group.Id;
        }
        
        AudioProgram audioProgram = new AudioProgram();
        audioProgram.Id = currentProgram.Id;
        audioProgram.Name = updateProgramName;
        audioProgram.Start = Convert.ToInt32(programDict["Start"]);
        audioProgram.Stop = Convert.ToInt32(programDict["Stop"]);
        audioProgram.Dow = _helpers.JArrayToArrayList(programDict["Dow"]);
        audioProgram.Url = (string)programDict["Url"];
        audioProgram.GroupName = updateGroupName;
        audioProgram.GroupId = groupId;
        bool found = _programsCollection.Update(audioProgram); //debug 'found'
        return currentProgram;
      } else {
        return null;
      }
    }
    /*
    private int DeleteGroup(int groupId) {
      return _groupsCollection.Delete(x => x.Id == groupId);
    }
    */
    private int AddGroup(string name) {
      AudioGroup group = new AudioGroup();
      group.Name = name;
      int id = _groupsCollection.Insert(group);
      return id;
    }

    private List<string> GetGroupNames() {
      IEnumerable<AudioGroup> groupsList = _groupsCollection.FindAll();
      List<string> groupNames = new List<string>();
      foreach(AudioGroup group in groupsList) {
        groupNames.Add(group.Name);
      }
      return groupNames;
    }
    
    private int AddSession(string groupName) {
      AudioSession session = new AudioSession();
      //get group id
      AudioGroup group = _groupsCollection.FindOne(x => x.Name.Equals(groupName));
      if(group != null) {
        session.GroupId = group.Id;
        session.GroupName = groupName;
        int id = _sessionCollection.Insert(session);
        return id;
      }
      return 0;
    }
    
    private string GetSession() {
      AudioSession session = _sessionCollection.FindOne(x => x.Id == 1);
      return session.GroupName;
    }

    private bool UpdateSession(string groupName) {
      AudioGroup group = _groupsCollection.FindOne(x => x.Name.Equals(groupName));
      if(group != null) {
        AudioSession audioSession = new AudioSession();
        audioSession.Id = 1;
        audioSession.GroupId = group.Id;
        audioSession.GroupName = groupName;
        return _sessionCollection.Update(audioSession);
      }
      return false;
    }
    
    private List<Dictionary<string, object>> GetPrograms(string groupName) {
      try {
        var programs = _programsCollection.Find(x => x.GroupName.Equals(groupName));
        List<Dictionary<string, object>> programsList = new List<Dictionary<string, object>>();
        
        foreach(AudioProgram program in programs) {
          programsList.Add(program.Program());
        }
        //sort programs by start time
        IOrderedEnumerable<Dictionary<string,object>> programsListSorted = programsList.OrderBy(program => program["Start"]);
        return programsListSorted.ToList();
      } catch(Exception ex) {
        Console.WriteLine(ex.Message);
        return new List<Dictionary<string, object>>();
      }
    }

    private void ProcessRecordings(string groupName) {
      try {
        _processingCancelled = false;
        _programsToRecord = new List<Dictionary<string, object>>();
        //get programs under the group
        List<Dictionary<string, object>> programsList = GetPrograms(groupName);
        if(programsList.Count > 0) {
          //what is today's dow
          DateTime today = DateTime.Now;
          long dow = Convert.ToInt64(today.DayOfWeek);
          int now24 = today.Hour * 100 + today.Minute - 1;

          //get the programs for today
          foreach(Dictionary<string, object> program in programsList) {
            ArrayList dowList = (ArrayList)(program["Dow"]);
            if(dowList.Contains(dow)) {
              //are we pass the stop time
              if(!((int)program["Stop"] < now24)) {
                _programsToRecord.Add(program);
                //send waiting message to client
                Dictionary<string, object> responseDict = new Dictionary<string, object>() {
                  {"action", "waiting"},
                  {"message", (string)program["Name"]}
                };
                _helpers.SendWsText(_wsFrameWriter, responseDict);
              }
            }
          }
          if(_programsToRecord.Count > 0) {
            Dictionary<string, object> programDict = (Dictionary<string, object>)_programsToRecord[0];
            string name = (string)programDict["Name"]; //debug 'name'
            int start = (int)(programDict["Start"]);

            SetStartDelay(start);
            _timer = Observable.Timer(TimeSpan.FromSeconds(_delay));
            _timer.Subscribe(
              x => RecordingStart(),
              ex => RecordingException(ex),
              () => RecordingCompleted()
            );
          } else {
            Dictionary<string, object> responseDict = new Dictionary<string, object>() {
              {"action", "error"},
              {"message", "Processing 0 recordings"}
            };
            _helpers.SendWsText(_wsFrameWriter, responseDict);
          }
        } 
      } catch(Exception ex) {
        Dictionary<string, object> responseDict = new Dictionary<string, object>() {
          {"action", "error"},
          {"message", ex.Message}
        };
        _helpers.SendWsText(_wsFrameWriter, responseDict);
        Console.WriteLine(ex.Message);
      }
    }
    private void RecordingStart(){
      Dictionary<string, object> programDict = _programsToRecord[_currentRecordingIdx];
      string name = (string)programDict["Name"];
      int start = Convert.ToInt32(programDict["Start"]);
      int stop = Convert.ToInt32(programDict["Stop"]);
      string url = (string)programDict["Url"];
      
      //compute duration
      DateTime now = DateTime.Now;
      int hour = stop / 100;
      int minute = stop - hour * 100;
      DateTime stopTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
      _duration = stopTime.Subtract(now).TotalSeconds;
      
      //debug
      Console.WriteLine($"Program: {name} Start: {start}  Stop: {stop}  Duration: {_duration} seconds");
      
      double timeInterval = _duration / 10.0;
      _recordingFinished = false;
      int bufferSize = 2048;
      int intervalCnt = 1;
      byte[] buffer = new byte[bufferSize];
      string outputfile = Path.Combine(@"c:\temp", $"{name}_{DateTime.Now.Month}_{DateTime.Now.Day}.mp3");
      
      
      //send start message to client
      Dictionary<string, object> responseDict = new Dictionary<string, object>() {
        {"action", "startRecord"},
        {"message", name}
      };
      _helpers.SendWsText(_wsFrameWriter, responseDict);

      try {
        FileStream fileStream = new FileStream(outputfile, System.IO.FileMode.Create, FileAccess.Write, FileShare.None,
          bufferSize: bufferSize, useAsync: false);
        HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);
        WebResponse response = webReq.GetResponse();

        //get the stream associated with response
        Stream responseStream = response.GetResponseStream();
        if (responseStream != null) {
          while (!_recordingFinished) {
            int bytesRead = responseStream.Read(buffer, 0, buffer.Length);
            fileStream.Write(buffer, 0, bytesRead);
            double elapsed = DateTime.Now.Subtract(_startTime).TotalSeconds;
            if (elapsed > _duration) {
              _recordingFinished = true;
              //Debug
              Console.WriteLine($"Program {name} finished. Elapsed time: {elapsed} seconds");
            }
            else {
              if (elapsed > timeInterval * intervalCnt) {
                responseDict = new Dictionary<string, object>() {
                  {"action", "progressRecord"},
                  {"message", intervalCnt}
                };
                _helpers.SendWsText(_wsFrameWriter, responseDict);
                intervalCnt += 1;
              }
            }
          }

          responseStream?.Close();
          fileStream?.Close();
          responseDict = new Dictionary<string, object>() {
            {"action", "endRecord"},
            {"message", name}
          };
          _helpers.SendWsText(_wsFrameWriter, responseDict);
          _currentRecordingIdx++;
        }
      } catch(Exception ex) {
        throw new Exception(ex.Message);
      } 
    }
    private void RecordingCompleted() {
      if(!_processingCancelled) {
        if(_currentRecordingIdx == _programsToRecord.Count) {
          Console.WriteLine("Processing Completed");
          Dictionary<string, object> responseDict = new Dictionary<string, object>() {
            {"action", "endProcessing"},
            {"message", "Processing Completed"}
          };
          _helpers.SendWsText(_wsFrameWriter, responseDict);
        } else {
          Dictionary<string, object> programDict = (Dictionary<string, object>)_programsToRecord[_currentRecordingIdx];
          string name = (string)programDict["Name"]; //debug 'name'
          int start = (int)programDict["Start"];
            
          SetStartDelay(start);
          _timer = Observable.Timer(TimeSpan.FromSeconds(_delay));
          _timer.Subscribe(
            x => RecordingStart(),
            ex => RecordingException(ex),
            () => RecordingCompleted()
          );
        }
      }
    }
    private void RecordingException(Exception exception) {
      Console.WriteLine($"Exception: {exception.Message}");
      Dictionary<string, object> responseDict = new Dictionary<string, object>() {
        {"action", "error"},
        {"message", $"Exception: {exception.Message}"}
      };
      _helpers.SendWsText(_wsFrameWriter, responseDict);
    }
    private void SetStartDelay(int start) {
      //convert start/stop times to DateTime
      DateTime now = DateTime.Now;
      int hour = start / 100;
      int minute = start - hour * 100;
      _startTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);

      //compute delay
      _delay = _startTime.Subtract(now).TotalSeconds;
      if(_delay < 0.0) {
        _delay = 0.0;
        _startTime = now;
      }
    }
  }
}