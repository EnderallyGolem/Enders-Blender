using System.Collections.Generic;
using System.Collections.Specialized;

namespace Celeste.Mod.EndHelper;

public class EndHelperModuleSaveData : EverestModuleSaveData {

    // Dictionary<string, string>. The one ordered one to determine order maps are saved.
    // This order only matters for limiting the number of saved maps (if limit reached, deletes from back of order)
    public OrderedDictionary mapDict_roomStatCustomNameDict = new OrderedDictionary { };
    public Dictionary<string, Dictionary<string, int>> mapDict_roomStat_colorIndex = [];

    // I have learnt from my mistakes and will store the room order in a non stupid way
    public Dictionary<string, List<string>> mapDict_roomStat_firstClear_roomOrder = [];
    public Dictionary<string, Dictionary<string, int>> mapDict_roomStat_firstClear_death = [];
    public Dictionary<string, Dictionary<string, long>> mapDict_roomStat_firstClear_timer = [];
    public Dictionary<string, Dictionary<string, int>> mapDict_roomStat_firstClear_strawberries = [];
    public Dictionary<string, Dictionary<string, bool>> mapDict_roomStat_firstClear_pauseType = [];



    public Dictionary<string, List<string>> mapDict_roomStat_latestSession_roomOrder = [];
    public Dictionary<string, Dictionary<string, int>> mapDict_roomStat_latestSession_death = [];
    public Dictionary<string, Dictionary<string, long>> mapDict_roomStat_latestSession_timer = [];
    public Dictionary<string, Dictionary<string, int>> mapDict_roomStat_latestSession_strawberries = [];
    public Dictionary<string, Dictionary<string, bool>> mapDict_roomStat_latestSession_pauseType = [];
}