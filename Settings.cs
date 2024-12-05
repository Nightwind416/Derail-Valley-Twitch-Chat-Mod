using UnityModManagerNet;

namespace TwitchChat {
    public class Settings : UnityModManager.ModSettings, IDrawable {
        [Draw("Message Duration (in seconds)")]
        public int messageDuration = 20;

        [Draw("Commands Active?")]
        public bool commandsActive = false;

        [Draw("Welcome Message")]
        public string welcomeMessage = "Hello, welcome to the stream!";

        [Draw("Info Message")]
        public string infoMessage = "This is a test message!";

        [Draw("Dispatcher Mod Active?")]
        public bool dispatcherActive = false;

        [Draw("Dispatcher Message")]
        public string dispatcherMessage = "Hello, welcome to the stream!";

        [Draw("Command Message")]
        public string commandMessage = "!help !commands";

        // [Draw("Each Timed Message will repeat every set # of seconds!")]

        [Draw("Timed Message 1 Toggle")]
        public bool timedMessage1Toggle = true;

        [Draw("Timed Message 1")]
        public string timedMessage1 = "Timed Message 1";

        [Draw("Timed Message 1 Timer (in seconds)")]
        public float timedMessage1Timer = 0;

        [Draw("Timed Message 2 Toggle")]
        public bool timedMessage2Toggle = true;

        [Draw("Timed Message 2")]
        public string timedMessage2 = "Timed Message 2";

        [Draw("Timed Message 2 Timer (in seconds)")]
        public float timedMessage2Timer = 0;

        [Draw("Timed Message 3 Toggle")]
        public bool timedMessage3Toggle = true;

        [Draw("Timed Message 3")]
        public string timedMessage3 = "Timed Message 3";

        [Draw("Timed Message 3 Timer (in seconds)")]
        public float timedMessage3Timer = 0;

        [Draw("Timed Message 4 Toggle")]
        public bool timedMessage4Toggle = true;

        [Draw("Timed Message 4")]
        public string timedMessage4 = "Timed Message 4";

        [Draw("Timed Message 4 Timer (in seconds)")]
        public float timedMessage4Timer = 0;

        [Draw("Timed Message 5 Toggle")]
        public bool timedMessage5Toggle = true;

        [Draw("Timed Message 5")]
        public string timedMessage5 = "Timed Message 5";

        [Draw("Timed Message 5 Timer (in seconds)")]
        public float timedMessage5Timer = 0;

        [Draw("Timed Message 6 Toggle")]
        public bool timedMessage6Toggle = true;

        [Draw("Timed Message 6")]
        public string timedMessage6 = "Timed Message 6";

        [Draw("Timed Message 6 Timer (in seconds)")]
        public float timedMessage6Timer = 0;

        [Draw("Timed Message 7 Toggle")]
        public bool timedMessage7Toggle = true;

        [Draw("Timed Message 7")]
        public string timedMessage7 = "Timed Message 7";

        [Draw("Timed Message 7 Timer (in seconds)")]
        public float timedMessage7Timer = 0;

        [Draw("Timed Message 8 Toggle")]
        public bool timedMessage8Toggle = true;

        [Draw("Timed Message 8")]
        public string timedMessage8 = "Timed Message 8";

        [Draw("Timed Message 8 Timer (in seconds)")]
        public float timedMessage8Timer = 0;

        [Draw("Timed Message 9 Toggle")]
        public bool timedMessage9Toggle = true;

        [Draw("Timed Message 9")]
        public string timedMessage9 = "Timed Message 9";

        [Draw("Timed Message 9 Timer (in seconds)")]
        public float timedMessage9Timer = 0;

        [Draw("Timed Message 10 Toggle")]
        public bool timedMessage10Toggle = true;

        [Draw("Timed Message 10")]
        public string timedMessage10 = "Timed Message 10";

        [Draw("Timed Message 10 Timer (in seconds)")]
        public float timedMessage10Timer = 0;

        public override void Save(UnityModManager.ModEntry entry) {
            Save(this, entry);
        }

        public void OnChange() { }
    }
}