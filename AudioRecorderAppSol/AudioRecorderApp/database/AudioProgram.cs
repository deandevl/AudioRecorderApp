using System.Collections;
using System.Collections.Generic;

namespace AudioRecorderApp.database {
  public class AudioProgram {
    public int Id {get;set;}
    public string Name {get;set;}
    public int Start {get;set;}
    public int Stop {get;set;}
    public ArrayList Dow {get;set;}
    public string Url {get;set;}
    public int GroupId {get;set;}
    public string GroupName {get;set;}

    public Dictionary<string, object> Program() {
      return new Dictionary<string, object>() {
        {"Id", Id},
        {"Name",Name},
        {"Start",Start},
        {"Stop",Stop},
        {"Dow",Dow},
        {"Url",Url},
        {"GroupId",GroupId},
        {"GroupName",GroupName}
      };
    }
  }
}