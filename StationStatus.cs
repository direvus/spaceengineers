static string panel1_name = "Text panel office 1";
static string panel2_name = "Text panel office 2";
static string panel3_name = "Text panel office 3";
static string panel4_name = "Text panel office 4";
static string reactors_name = "Reactors";
static string batteries_name = "Batteries";
static string ingot_type = "VRage.Game.MyObjectBuilder_Ingot";
static string ore_type = "VRage.Game.MyObjectBuilder_Ore";

public struct ItemType {
    public string maintype, subtype;

    public ItemType (string main, string sub) {
        maintype = main;
        subtype = sub;
    }
}

ItemType fuel_type = new ItemType(ingot_type, "Uranium");
ItemType ice_type = new ItemType(ore_type, "Ice");

IMyTextPanel panel1, panel2, panel3, panel4;
IMyBlockGroup reactor_group, battery_group;

public Program() {
    panel1 = GridTerminalSystem.GetBlockWithName(panel1_name) as IMyTextPanel;
    panel2 = GridTerminalSystem.GetBlockWithName(panel2_name) as IMyTextPanel;
    panel3 = GridTerminalSystem.GetBlockWithName(panel3_name) as IMyTextPanel;
    panel4 = GridTerminalSystem.GetBlockWithName(panel4_name) as IMyTextPanel;
    reactor_group = GridTerminalSystem.GetBlockGroupWithName(reactors_name);
    battery_group = GridTerminalSystem.GetBlockGroupWithName(batteries_name);
}

public void Main(string argument) {
    WriteText(panel1, GetPowerStatus());
    WriteText(panel2, GetOxygenStatus());
    WriteText(panel3, GetRefineryStatus());
    WriteText(panel4, GetMetalList());
}

public void WriteText(IMyTextPanel panel, string value) {
    panel.WritePublicText(value, false);
    panel.ShowPublicTextOnScreen();
}

public string GetPowerStatus() {
    string result = "";
    if (reactor_group == null) {
        result += "No reactors!\n";
    } else {
        var reactors = new List<IMyReactor>();
        reactor_group.GetBlocksOfType(reactors);
        for (int i = 0; i < reactors.Count; i++) {
            IMyReactor reactor = reactors[i];
            float fuel = TotalAmountOfType(reactor, fuel_type);
            result += String.Format("{0}:\n  {1:F2}MW - {2:P}\n  {3:F3} kg U",
                reactor.CustomName,
                reactor.CurrentOutput,
                reactor.CurrentOutput / reactor.MaxOutput,
                fuel);
        }
    }

    if (battery_group == null) {
        result += "\n\nNo batteries.";
    } else {
        result += "\n\nBatteries:\n";
        var batteries = new List<IMyBatteryBlock>();
        battery_group.GetBlocksOfType(batteries);
        for (int i = 0; i < batteries.Count; i++) {
            IMyBatteryBlock battery = batteries[i];
            string status = "";
            if (battery.IsCharging) {
                status = "Charging";
            }
            result += String.Format("  {0:P} {1}\n",
                battery.CurrentStoredPower / battery.MaxStoredPower,
                status);
        }
    }
    return result;
}

public string GetOxygenStatus() {
    var gens = new List<IMyGasGenerator>();
    GridTerminalSystem.GetBlocksOfType(gens);
    string result = String.Format("{0:F2} kg H2O\n\n",
            TotalAmountOfType(gens, ice_type));

    var tanks = new List<IMyGasTank>();
    GridTerminalSystem.GetBlocksOfType(tanks);
    if (tanks.Count > 0) {
        result += "O2 tanks:\n";
        for (int i = 0; i < tanks.Count; i++) {
            result += String.Format("  {0:P}\n",
                    tanks[i].FilledRatio);
        }
    } else {
        result = "None\n";
    }
    return result;
}

public string GetRefineryStatus() {
    string result;
    var refineries = new List<IMyRefinery>();
    GridTerminalSystem.GetBlocksOfType(refineries);
    if (refineries.Count > 0) {
        bool active = false;
        string queue = "";
        string status;

        for (int i = 0; i < refineries.Count; i++) {
            if (refineries[i].IsProducing) {
                active = true;
            }
            for (int j = 0; j < refineries[i].InventoryCount; j++) {
                var inventory = refineries[i].GetInventory(j);
                var items = inventory.GetItems();
                for (int k = 0; k < items.Count; k++) {
                    if (IsMainType(items[k], ore_type)) {
                        queue += String.Format("{0:F3} {1}\n",
                                items[k].Amount,
                                items[k].Content.SubtypeName);
                    }
                }
            }
        }
        if (active) {
            status = "ACTIVE";
        } else {
            status = "IDLE";
        }
        result = String.Format("Refineries: {0}\n", status) + queue;
    } else {
        result = "No refineries!";
    }
    return result;
}

public string GetMetalList() {
    string result = "Metals:\n";
    var blocks = new List<IMyTerminalBlock>();
    var totals = new Dictionary<string, float>();
    GridTerminalSystem.GetBlocksOfType(blocks);

    for (int i = 0; i < blocks.Count; i++) {
        for (int j = 0; j < blocks[i].InventoryCount; j++) {
            var inventory = blocks[i].GetInventory(j);
            var items = inventory.GetItems();
            for (int k = 0; k < items.Count; k++) {
                if (!IsMainType(items[k], ingot_type)) {
                    continue;
                }
                string subtype = items[k].Content.SubtypeName;
                float amount = (float) items[k].Amount;
                if (totals.ContainsKey(subtype)) {
                    totals[subtype] += amount;
                } else {
                    totals[subtype] = amount;
                }
            }
        }
    }
    var pairs = totals.ToList();
    for (int i = 0; i < pairs.Count; i++) {
        result += String.Format("{0:F3} kg {1}\n",
                pairs[i].Value,
                pairs[i].Key);
    }
    return result;
}

public bool IsMainType(IMyInventoryItem item, string maintype) {
    return (item.Content.ToString() == maintype);
}

public bool IsMainType(IMyInventoryItem item, ItemType type) {
    return IsMainType(item, type.maintype);
}

public bool IsSubType(IMyInventoryItem item, ItemType type) {
    return (item.Content.SubtypeName == type.subtype);
}

public bool IsType(IMyInventoryItem item, ItemType type) {
    return IsMainType(item, type) && IsSubType(item, type);
}

public float TotalAmountOfType(IMyTerminalBlock block, ItemType type) {
    float result = 0;
    for (int i = 0; i < block.InventoryCount; i++) {
        var inventory = block.GetInventory(i);
        var items = inventory.GetItems();
        for (int j = 0; j < items.Count; j++) {
            if (IsType(items[j], type)) {
                result += (float) items[j].Amount;
            }
        }
    }
    return result;
}

public float TotalAmountOfType<T>(List<T> blocks, ItemType type)
        where T:IMyTerminalBlock {
    float result = 0;
    for (int i = 0; i < blocks.Count; i++) {
        result += TotalAmountOfType(blocks[i], type);
    }
    return result;
}

public string ListContents(IMyTerminalBlock block) {
    string result = "";
    for (int i = 0; i < block.InventoryCount; i++) {
        var inventory = block.GetInventory(i);
        var items = inventory.GetItems();
        for (int j = 0; j < items.Count; j++) {
            result += String.Format("{0:F3} {1}/{2}\n",
                    items[j].Amount,
                    items[j].Content.ToString(),
                    items[j].Content.SubtypeName);
        }
    }
    return result;
}
