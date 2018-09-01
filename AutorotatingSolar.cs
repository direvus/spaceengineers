String solar_name = "Solar Panel Port";
String rotor_name = "Rotor Solar Port";
String text_name = "LCD Panel";

IMyTextPanel text;
IMySolarPanel solar;
IMyMotorStator rotor;
float last = 0;

public Program() {
     text = GridTerminalSystem.GetBlockWithName(text_name) as IMyTextPanel;
     solar = GridTerminalSystem.GetBlockWithName(solar_name) as IMySolarPanel;
     rotor = GridTerminalSystem.GetBlockWithName(rotor_name) as IMyMotorStator;
}

public void Main(string argument) {
    float output = solar.MaxOutput;
    if(output != 0 && last != 0) {
        if (output < last) {
            rotor.ApplyAction("Reverse");
        }
    }
    WriteText(String.Format("\nPrevious output: {0:G3}\nCurrent output: {1:G3}\nRotor velocity: {2}",
                last,
                output,
                rotor.Velocity));
    last = output;
}

public void WriteText(string value) {
    text.WritePublicText(value, false);
    text.ShowPublicTextOnScreen();
}
