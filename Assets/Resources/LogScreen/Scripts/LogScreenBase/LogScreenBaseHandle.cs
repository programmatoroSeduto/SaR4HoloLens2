using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using TMPro;

// using Packages.Interfaces.Interfaces;
using Packages.VisualItems.Types;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;




namespace Packages.VisualItems.LogScreen.Components
{
    public class LogScreenBaseHandle : LogWindowTitleContentType
    {
        public int MaxLines = 8;
        public int MaxChars = 57;
        public int TitleMaxChars = 30;

        [Tooltip("If false, the defalt content will beprined inside the window (use this option to prevent unordered events calling issue)")]
        public bool PresetComponent = false;

        public string InitTitle = "";

        [TextArea(8, 57)]
        public string InitContent = "";

        private List<string> lines = new List<string>();
        private int FrameFirstLine = 0;
        private int FrameLastLine = 0;

        private bool ready = false;

        // Start is called before the first frame update
        void Start()
        {
            FrameFirstLine = 0;
            FrameLastLine = MaxLines - 1;

            if (PresetComponent)
            {
                EVENT_LogTitle(InitTitle);
                EVENT_LogContent(InitContent);
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        public override void EVENT_LogContent(string txt)
        {
            CollectLinesFromText(txt);

            string ss = "";
            for (int i = lines.Count - 1; i >= Mathf.Max(lines.Count - MaxLines, 0); i--)
                ss = lines[i] + '\n' + ss;

            FrameFirstLine = lines.Count - MaxLines;
            FrameLastLine = lines.Count - 1;
            transform.Find("LogScreenBody/LogScreenContent").GetComponent<TextMeshPro>().text = (ss != "" ? ss.Substring(0, ss.Length - 1) : "");
        }

        public override void EVENT_LogTitle(string txt)
        {
            // Debug.Log($"EVENT_LogTitle(txt: {txt})");
            if (txt.Length > TitleMaxChars)
                txt = txt.Substring(0, TitleMaxChars - 3) + "...";
            transform.Find("LogScreenBody/LogScreenTitle").GetComponent<TextMeshPro>().text = txt;
        }

        public void EVENT_SolverOffset(Vector3 offset)
        {
            transform.Find("LogScreenBody").GetComponent<Orbital>().LocalOffset = offset;
        }

        public void EVENT_Show()
        {
            transform.Find("LogScreenBody").gameObject.SetActive(true);
        }

        public void EVENT_Hide()
        {
            transform.Find("LogScreenBody").gameObject.SetActive(false);
        }

        private void CollectLinesFromText(string txt)
        {
            string[] lns = txt.Split('\n');
            foreach (string ln in lns)
            {
                string[] l = breakLine(ln, MaxChars);
                lines.AddRange(l);
            }
        }

        private string[] breakLine(string line, int maxLen)
        {
            if (line.Length == 0)
                return new string[] { line };

            string ss = "";
            for (int i = 0; i < line.Length; i += maxLen)
                ss = ss + line.Substring(i, Mathf.Min(maxLen, line.Length - i)) + '\n';
            ss = ss.Substring(0, ss.Length - 1);

            return ss.Split('\n');
        }
    }
}