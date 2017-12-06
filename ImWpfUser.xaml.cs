using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImWpf;

namespace ImWpfUser
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (o, e) => Main();
            Loaded += async (o, e) => Main2();
            Task.Run(async () =>
            {
                while (true)
                {
                    t++;
                    await Task.Delay(1);
                }
            });
        }

        public int y = 0;
        public int t = 0;
        private ImFormsMgr rightPanelMgr;

        private async Task Main()
        {
            ImFormsMgr mgr = new ImFormsMgr(ImWpfPanelLeft.Children);
            IList<int> list = new List<int> { 1, 2, 3 };
            int x = 0;
            bool displayList = false;
            bool reverseList = false;

            while (true)
            {
                mgr.Text("This ImForms panel refreshes only when there is user interaction");
                mgr.Space(CompileTime.ID());
                mgr.Text("ImForms makes it easy to display and modify one value with multiple controls");
                mgr.Text("x =");
                mgr.RadioButton("0", ref x, 0);
                mgr.RadioButton("1", ref x, 1);

                int valueToAssignX = (x == 1) ? 0 : 1;
                if (mgr.Button("x <- " + valueToAssignX, CompileTime.ID()))
                {
                    x = valueToAssignX;
                }

                bool xIs1 = (x == 1);
                mgr.Checkbox("X == 1", ref xIs1);
                x = xIs1 ? 1 : 0;

                mgr.Space(CompileTime.ID());
                mgr.Text("Just like with other ImGui implementations, if a function isn't called for it," +
                    " a control isn't displayed.");
                mgr.Checkbox("Show list", ref displayList);

                if (displayList)
                {
                    var seq = reverseList ? list.Reverse() : list;

                    if (mgr.Button("Add to end")) { list.Add(list.LastOrDefault() + 1); }

                    if (list.Any() && mgr.Button("Remove from front"))
                    {
                        list.RemoveAt(0);
                    }

                    mgr.Checkbox("Display reversed", ref reverseList);

                    foreach (var n in seq) { mgr.Text("[" + n + "]"); }
                }

                mgr.Space(CompileTime.ID());
                mgr.Text("Values from other threads can be displayed when a panel refreshes.");
                mgr.LinkText("Try it!");
                mgr.Text("y = " + y, CompileTime.ID());

                await mgr.NextFrame();
            }
        }

        private async Task Main2()
        {

            rightPanelMgr = new ImFormsMgr(ImWpfPanelRight.Children);

            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += (o, e) => rightPanelMgr.Refresh();
            int updateRate = 1000;

            while (true)
            {
                rightPanelMgr.Text("This ImForms panel auto-updates once every:");
                rightPanelMgr.RadioButton("Second", ref updateRate, 1000);
                rightPanelMgr.RadioButton("100ms", ref updateRate, 100);
                rightPanelMgr.RadioButton("10ms", ref updateRate, 10);
                rightPanelMgr.RadioButton("Never", ref updateRate, -1);
                timer.Interval = TimeSpan.FromMilliseconds( (updateRate > 0) ? updateRate : int.MaxValue );
                timer.IsEnabled = (updateRate > 0);
                rightPanelMgr.Space(CompileTime.ID());
                rightPanelMgr.Text("Auto-updating is an easy way to display values from other threads");
                rightPanelMgr.Text("y = " + y, CompileTime.ID());
                rightPanelMgr.Text("t = " + t, CompileTime.ID());
                await rightPanelMgr.NextFrame();
            }
        }

        private void YIncrBtn_Click(object sender, RoutedEventArgs e)
        {
            y++;
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {

            rightPanelMgr.Refresh();
        }
    }
}
