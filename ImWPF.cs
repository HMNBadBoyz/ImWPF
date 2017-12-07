using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CmplTime = System.Runtime.CompilerServices;
using Wpf = System.Windows;
using WpfElement = System.Windows.UIElement;
using WpfCtrls = System.Windows.Controls;
using WpfElementList = System.Windows.Controls.UIElementCollection;
using WpfDocs = System.Windows.Documents;
using WpfHandler = System.Windows.RoutedEventHandler;

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
        public class ImElement
        {
            public enum State
            {
                NotDrawn,
                Drawn
            }

            public readonly WpfElement WpfElement;
            public State ImState { get; set; }
            public int SortKey { get; set; }
            public string ID { get { return WpfElement.Uid; } }
            public WpfHandler Handler;
            public WpfUnsubber Unsubber = (_, __) => { };

            public ImElement(WpfElement wpfElement, WpfHandler del)
            {
                WpfElement = wpfElement;
                Handler = del;
            }
        }

        public delegate void WpfUnsubber(WpfElement element, WpfHandler handler);

        public struct ElmtMakerTuple
        {
            public readonly WpfElement WpfElement;
            public readonly WpfHandler Handler;
            public readonly WpfUnsubber Unsubber;

            public ElmtMakerTuple(WpfElement wpfElement, WpfHandler handler, WpfUnsubber unsubber)
            {
                WpfElement = wpfElement;
                Handler = handler;
                Unsubber = unsubber;
            }

            public static ElmtMakerTuple WithNoEvtHandling(WpfElement wpfElement)
            {
                return new ElmtMakerTuple(wpfElement, null, null);
            }
        }

        public delegate ElmtMakerTuple imFormsElmtMaker(string id);

        private int RemainingRedraws = 0;
        private TaskCompletionSource<bool> TCS;
        private Dictionary<string, ImElement> ImElements;
        public WpfElementList DisplayedElements;
        private int CurrentSortKey;
        private string InteractedElementId;

        // OH NOTE This could be configurable by the user in the _distant_ future
        private int RedrawsPerInteraction = 1;

        public ImFormsMgr(WpfElementList collection)
        {
            InteractedElementId = null;
            ImElements = new Dictionary<string, ImElement>();
            TCS = new TaskCompletionSource<bool>();
            CurrentSortKey = 0;
            DisplayedElements = collection;
        }

        public void QueueRedraws(int numRedraws) { RemainingRedraws += numRedraws; }

        public ElmtMakerTuple ButtonLikeMaker<TBtn>(string id)
            where TBtn : WpfCtrls.Primitives.ButtonBase, new()
        {
            var btn = new TBtn() { Uid = id };
            var handler = new WpfHandler(LetImGuiHandleIt);
            btn.Click += handler;
            return new ElmtMakerTuple(btn, handler,
                (btn1, hndl1) => ((TBtn)btn1).Click -= hndl1); // closure-free lambda
        }

        public WpfDocs.Hyperlink TryGetHyperlink(WpfElement wpfElement)
        {
            var tb = (wpfElement as WpfCtrls.TextBlock);
            if (tb != null)
            {
                var firstInline = tb.Inlines.FirstInline;
                if (firstInline != null)
                {
                    return firstInline as WpfDocs.Hyperlink;
                }
            }
            return null;
        }

        public void UnsubLinkTextBlock(WpfElement element, WpfHandler handler)
        {
            TryGetHyperlink(element).Click -= handler;
        }

        public ElmtMakerTuple LinkTextBlockMaker(string id)
        {
            var link = new WpfDocs.Hyperlink(new WpfDocs.Run());
            // Easiest way to do this is to capture id in a closure
            WpfCtrls.TextBlock linktxt = new WpfCtrls.TextBlock(link) { Uid = id };
            WpfHandler handler = (o, e) =>
            {
                InteractedElementId = id;
                QueueRedraws(RedrawsPerInteraction);
                TCS.SetResult(true);
            };
            link.Click += handler;
            return new ElmtMakerTuple(linktxt, handler, UnsubLinkTextBlock);
        }

        public void LetImGuiHandleIt(object sender, Wpf.RoutedEventArgs args)
        {
                InteractedElementId = ((WpfCtrls.Control)sender).Uid;
                QueueRedraws(RedrawsPerInteraction);
                TCS.SetResult(true);
        }

        public ImElement ProcureElement(string id, imFormsElmtMaker maker)
        {
            ImElement elmt;
            if (!ImElements.TryGetValue(id, out elmt))
            {
                ElmtMakerTuple t = maker(id);
                elmt = new ImElement(t.WpfElement, t.Handler);
                ImElements.Add(id, elmt);
            }

            elmt.ImState = ImElement.State.Drawn;
            elmt.SortKey = CurrentSortKey;
            elmt.WpfElement.Visibility = Wpf.Visibility.Visible ;
            CurrentSortKey++;
            return elmt;
        }

        public void Space(string id)
        {
            Text("", id);
        }

        public void Text(string text, string id = null)
        {
            if (id == null) id = text;
            var elmt = ProcureElement(
                id ?? text, 
                id1 => ElmtMakerTuple.WithNoEvtHandling(new WpfCtrls.TextBlock() { Uid = id }));
            ((WpfCtrls.TextBlock)elmt.WpfElement).Text = text;
        }

        public bool Button(string text, string id = null)
        {
            var elmt = ProcureElement(id ?? text, ButtonLikeMaker<WpfCtrls.Button>);
            var Button = (WpfCtrls.Button)elmt.WpfElement;
            Button.Content = text;
            return InteractedElementId == elmt.ID;
        }
        
        public bool LinkText(string text, string id = null)
        {
            var elmt = ProcureElement(id ?? text, LinkTextBlockMaker);
            var hyperlink = (WpfDocs.Hyperlink)((WpfCtrls.TextBlock)elmt.WpfElement).Inlines.FirstInline;
            ((WpfDocs.Run)hyperlink.Inlines.FirstInline).Text = text;
            // NOTE: this works because the Hyperlink's event handler explicitly assigns id
            return InteractedElementId == elmt.ID;
        }

        public bool Checkbox(string text, ref bool checkBoxChecked, string id = null)
        {
            var elmt = ProcureElement(id ?? text, ButtonLikeMaker<WpfCtrls.CheckBox>);
            var checkBox = (WpfCtrls.CheckBox)elmt.WpfElement;
            checkBox.Content = text;
            var wasInteracted = InteractedElementId == elmt.ID;

            if (wasInteracted) { checkBoxChecked = checkBox.IsChecked.GetValueOrDefault(); }
            else { checkBox.IsChecked = checkBoxChecked; }

            return wasInteracted;
        }

        public bool RadioButton(string text, ref int value, int checkAgainst, string id = null)
        {
            var elmt = ProcureElement(id ?? text, ButtonLikeMaker<WpfCtrls.RadioButton>);
            var radioButton = (WpfCtrls.RadioButton)elmt.WpfElement;
            radioButton.Content = text;
            var wasInteracted = InteractedElementId == elmt.ID;

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
            const int elmtsToTriggerCleanup = 100;
            const int elmtsToCleanup = 50;

            var undrawnElements = ImElements.Values.Where(elmt => elmt.ImState == ImElement.State.NotDrawn)
                .Take(elmtsToTriggerCleanup).ToList();

            if (undrawnElements.Count == elmtsToTriggerCleanup)
            {
                foreach (var elmt in undrawnElements.Take(elmtsToCleanup))
                {
                    elmt.Unsubber(elmt.WpfElement, elmt.Handler);
                    ImElements.Remove(elmt.ID);
                }
            }

            InteractedElementId = null;
            WpfElement[] sortedWpfElements = ImElements.Values.Where(x => x.ImState == ImElement.State.Drawn)
                .OrderBy(c => c.SortKey).Select(c => c.WpfElement).ToArray();
            var wpfElementsChanged = DisplayedElements.Count != sortedWpfElements.Length
                || !Enumerable.Zip(
                        DisplayedElements.OfType<WpfElement>(),
                        sortedWpfElements,
                        (c1, c2) => c1 == c2).All(b => b);

            if (wpfElementsChanged)
            {
                DisplayedElements.Clear();
                foreach (var item in sortedWpfElements)
                {
                    DisplayedElements.Add(item);
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

            foreach (var elmt in ImElements.Values)
            {
                elmt.ImState = ImElement.State.NotDrawn;
                elmt.SortKey = 999999;
            }
        }
    }
}