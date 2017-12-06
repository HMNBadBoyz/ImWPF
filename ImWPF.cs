using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CmplTime = System.Runtime.CompilerServices;
using Wpf = System.Windows;
using WpfUiElmt = System.Windows.UIElement;
using WpfCtrls = System.Windows.Controls;
using WpfUiElmtList = System.Windows.Controls.UIElementCollection;
using WpfDocs = System.Windows.Documents;

namespace ImWpf
{
    public static class CompileTime
    {
        public static string ID(
            [CmplTime.CallerFilePath] string srcFilePath = "",
            [CmplTime.CallerLineNumber] int srcLineNumber = 0)
        {
            return srcFilePath + srcLineNumber;
        }
    }

    public class ImFormsMgr
    {
        public class Ctrl
        {
            public enum State
            {
                NotDrawn,
                Drawn
            }

            public readonly WpfUiElmt WpfElmt;
            public State CtrlState { get; set; }
            public int SortKey { get; set; }
            public string ID { get { return WpfElmt.Uid; } }

            public Ctrl(WpfUiElmt control) { WpfElmt = control; }
        }

        private int RemainingRedraws = 0;
        private TaskCompletionSource<bool> TCS;
        private Dictionary<string, Ctrl> ImControls;
        public WpfUiElmtList DisplayedControls;
        private int CurrentSortKey;
        private string InteractedElementId;

        // OH NOTE This could be configurable by the user in the _distant_ future
        private int RedrawsPerInteraction = 1;

        public ImFormsMgr(WpfUiElmtList collection)
        {
            InteractedElementId = null;
            ImControls = new Dictionary<string, Ctrl>();
            TCS = new TaskCompletionSource<bool>();
            CurrentSortKey = 0;
            DisplayedControls = collection;
        }

        public void QueueRedraws(int numRedraws) { RemainingRedraws += numRedraws; }


        public delegate WpfUiElmt ImFormsCtrlMaker(string id);

        public WpfUiElmt ClickCtrlMaker<TCtrl>(string id) where TCtrl : WpfCtrls.Primitives.ButtonBase, new()
        {
            var wfCtrl = new TCtrl() { Uid = id };
            wfCtrl.Click += LetImGuiHandleIt;
            return wfCtrl;
        }

        public WpfCtrls.TextBlock LinkLabelMaker(string id)
        {
            var link = new WpfDocs.Hyperlink(new WpfDocs.Run());
            // Easiest way to do this is to capture id in a closure
            WpfCtrls.TextBlock linktxt = new WpfCtrls.TextBlock(link) { Uid = id };
            link.Click += (o, e) =>
            {
                InteractedElementId = id;
                QueueRedraws(RedrawsPerInteraction);
                TCS.SetResult(true);
            };
            return linktxt;
        }

        public void LetImGuiHandleIt(object sender, Wpf.RoutedEventArgs args)
        {
                InteractedElementId = ((WpfCtrls.Control)sender).Uid;
                QueueRedraws(RedrawsPerInteraction);
                TCS.SetResult(true);
        }

        public Ctrl ProcureControl(string id, ImFormsCtrlMaker maker)
        {
            Ctrl ctrl;
            if (!ImControls.TryGetValue(id, out ctrl))
            {
                ctrl = new Ctrl(maker(id));
                ImControls.Add(id, ctrl);
            }

            ctrl.CtrlState = Ctrl.State.Drawn;
            ctrl.SortKey = CurrentSortKey;
            ctrl.WpfElmt.Visibility = Wpf.Visibility.Visible ;
            CurrentSortKey++;
            return ctrl;
        }

        public void Space(string id)
        {
            Text("", id);
        }

        public void Text(string text, string id = null)
        {
            if (id == null) id = text;
            var ctrl = ProcureControl(id ?? text, id1 => new WpfCtrls.TextBlock() { Uid = id });
            ((WpfCtrls.TextBlock)ctrl.WpfElmt).Text = text;
        }

        public bool Button(string text, string id = null)
        {
            var ctrl = ProcureControl(id ?? text, ClickCtrlMaker<WpfCtrls.Button>);
            var Button = (WpfCtrls.Button)ctrl.WpfElmt;
            Button.Content = text;
            return InteractedElementId == ctrl.ID;
        }
        
        public bool LinkText(string text, string id = null)
        {
            var ctrl = ProcureControl(id ?? text, LinkLabelMaker);
            var hyperlink = (WpfDocs.Hyperlink)((WpfCtrls.TextBlock)ctrl.WpfElmt).Inlines.FirstInline;
            ((WpfDocs.Run)hyperlink.Inlines.FirstInline).Text = text;
            // NOTE: this works because the Hyperlink's event handler explicitly assigns id
            return InteractedElementId == ctrl.ID;
        }

        public bool Checkbox(string text, ref bool checkBoxChecked, string id = null)
        {
            var ctrl = ProcureControl(id ?? text, ClickCtrlMaker<WpfCtrls.CheckBox>);
            var checkBox = (WpfCtrls.CheckBox)ctrl.WpfElmt;
            checkBox.Content = text;
            var wasInteracted = InteractedElementId == ctrl.ID;

            if (wasInteracted) { checkBoxChecked = checkBox.IsChecked.GetValueOrDefault(); }
            else { checkBox.IsChecked = checkBoxChecked; }

            return wasInteracted;
        }

        public bool RadioButton(string text, ref int value, int checkAgainst, string id = null)
        {
            var ctrl = ProcureControl(id ?? text, ClickCtrlMaker<WpfCtrls.RadioButton>);
            var radioButton = (WpfCtrls.RadioButton)ctrl.WpfElmt;
            radioButton.Content = text;
            var wasInteracted = InteractedElementId == ctrl.ID;

            if (wasInteracted) { value = checkAgainst; }
            else { radioButton.IsChecked = (value == checkAgainst); }

            return wasInteracted;
        }

        public void Refresh()
        {
            TCS.TrySetResult(true);
        }

        public async Task NextFrame()
        {
            // PrevInteractedElement = InteractedElement;
            const int ctrlsToTriggerCleanup = 100;
            const int ctrlsToRemoveForCleanup = 50;

            var undrawnControls = ImControls.Values.Where(ctrl => ctrl.CtrlState == Ctrl.State.NotDrawn)
                .Take(ctrlsToTriggerCleanup).ToList();

            if (undrawnControls.Count == ctrlsToTriggerCleanup)
            {
                foreach (var ctrl in undrawnControls.Take(ctrlsToRemoveForCleanup))
                {
                    ImControls.Remove(ctrl.ID);
                }
            }

            InteractedElementId = null;
            WpfUiElmt[] sortedControls = ImControls.Values.Where(x => x.CtrlState == Ctrl.State.Drawn)
                .OrderBy(c => c.SortKey).Select(c => c.WpfElmt).ToArray();
            var controlsChanged = DisplayedControls.Count != sortedControls.Length
                || !Enumerable.Zip(
                        DisplayedControls.OfType<WpfCtrls.Control>(),
                        sortedControls,
                        (c1, c2) => c1 == c2).All(b => b);

            if (controlsChanged)
            {
                DisplayedControls.Clear();
                foreach (var item in sortedControls)
                {
                    DisplayedControls.Add(item);
                }
            }

            // Automatically go to next frame for each requested redraw
            if (RemainingRedraws <= 0)
            {
                RemainingRedraws = 0;
                await TCS.Task;
                TCS = new TaskCompletionSource<bool>();
            }
            else
            {
                RemainingRedraws--;
            }

            foreach (var ctrl in ImControls.Values)
            {
                ctrl.CtrlState = Ctrl.State.NotDrawn;
                ctrl.SortKey = 999999;
            }
        }
    }
}