# ImWpf
ImWpf is an experiment in creating a wrapper class that allows you to create and get input from WPF controls, but in an fashion similar to Immediate Mode GUI systems such as [Dear ImGui](https://github.com/ocornut/imgui). It is compatible with existing WPF projects. You need only provide the `ImWpfMgr` constructor with a Panel in which to place its controls.

[![https://gyazo.com/e6368c59ea442986d7eef8c2666a7e53](https://i.gyazo.com/e6368c59ea442986d7eef8c2666a7e53.gif)](https://gyazo.com/e6368c59ea442986d7eef8c2666a7e53)

It is a fairly close port of [ImForms](https://github.com/HMNBadBoyz/ImForms) and was coded primarily by Mohamed Shazan.

ImWpf could someday become a minimal-dependency tool that can easily be integrated into an existing WPF (.NET 4.5+) project, and allows developers to create GUIs with minimal work.

The class is in ImWpf.cs. It's simple to use. Just follow the example project (ImWpfUser.xaml\*). The biggest gotcha is that you get the best performance if you assign every control a unique string identifier. The function `CompileTime.ID()` makes this easy. The second biggest gotcha is that there just aren't very many features at the moment.

This project is just an experiment. If you want to see it become more fully-featured, let Ozzy know on [Twitter](https://twitter.com/OswaldHurlem).