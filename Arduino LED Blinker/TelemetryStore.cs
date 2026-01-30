using System;
using System.Collections.Generic;

namespace UnoLedControl
{
    public class TelemetryStore
    {
        private readonly object _lock = new object();
        private readonly Dictionary<string, string> _kv = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void UpdateFromTelemetryLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            line = line.Trim();

            // Accept lines like:
            // "T key=value ..."
            // "RX: T key=value ..."
            int tIndex = line.IndexOf("T ");
            if (tIndex < 0) return;

            string payload = line.Substring(tIndex + 2).Trim(); // everything after "T "
            if (payload.Length == 0) return;

            string[] parts = payload.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            lock (_lock)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    int eq = parts[i].IndexOf('=');
                    if (eq <= 0 || eq >= parts[i].Length - 1) continue;

                    string key = parts[i].Substring(0, eq).Trim();
                    string val = parts[i].Substring(eq + 1).Trim();
                    if (key.Length == 0) continue;

                    _kv[key] = val;
                }

                _kv["last_rx"] = DateTime.Now.ToString("HH:mm:ss");
            }
        }


        public string Get(string key, string fallback = "--")
        {
            lock (_lock)
            {
                string v;
                if (_kv.TryGetValue(key, out v)) return v;
                return fallback;
            }
        }
    }
}
