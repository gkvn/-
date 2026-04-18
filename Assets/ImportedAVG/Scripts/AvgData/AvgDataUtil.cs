namespace AVG {
  public static class AvgDataUtil {

    public static void SetVariable(string varName, float value) {
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return;
      }
      var vars = dataManager.vars;
      if (vars == null) {
        return;
      }
      vars.TryAdd(varName, value);
    }
    
    public static float GetVariable(string varName) {
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return 0;
      }
      var vars = dataManager.vars;
      if (vars == null) {
        return 0;
      }
      if (vars.TryGetValue(varName, out var value)) {
        return value;
      }
      return 0;
    }
    
    public static void AddFlag(string varName) {
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return;
      }
      var flags = dataManager.flags;
      if (flags == null) {
        return;
      }
      flags.Add(varName);
    }
    
    public static void RemoveFlag(string varName) {
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return;
      }
      var flags = dataManager.flags;
      if (flags == null) {
        return;
      }
      flags.Remove(varName);
    }
    
    public static bool CheckFlag(string varName) {
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return false;
      }
      var flags = dataManager.flags;
      if (flags == null) {
        return false;
      }
      return flags.Contains(varName);
    }

    public static void SetVariable(SetVar item) {
      if (item == null || string.IsNullOrEmpty(item.varName)) {
        return;
      }
      var dataManager = AvgController.Instance?.dataManager;
      if (dataManager == null) {
        return;
      }
      var vars = dataManager.vars;
      if (vars == null) {
        return;
      }
      
      float currentValue = vars.TryGetValue(item.varName, out var value) ? value : 0;
      float newValue = currentValue;
      
      switch (item.opt) {
        case SetVar.Operation.SET:
          newValue = item.num;
          break;
        case SetVar.Operation.ADD:
          newValue = currentValue + item.num;
          break;
        case SetVar.Operation.MINUS:
          newValue = currentValue - item.num;
          break;
      }
      
      vars[item.varName] = newValue;
    }
  }
}