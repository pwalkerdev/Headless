# Headless

A simple, barebones foundation for interpreting, compiling & executing code.

Also check out [prototyper](https://github.com/pwalkerdev/prototyper). A VSCode extension built using Headless.

## Description

The goal of the Headless project is very simple: A convinient platform to write & test code quickly, with minimal setup - preferably none. No need to create a new solution, scaffold a database, use a project template or install a gigabyte of npm packages - matter of fact, you don't even have to save it to file. Just take a chunk of code, run it, and examine the output. From there, an idea can be proven, and _then_ you can start farting about with that other stuff.

Or, maybe it's not an idea for a new product, feature or service. Maybe you just want to test a theory, better understand the behavior of a particular language feature or just run a couple of lines of code to see what happens. Upshot is, there's a long list of valid use-cases for the ability to quickly and easily test atomic chunks of code.

Headless is a framework that aims to deliver a straightforward means of script interpretation and execution. You feed Headless a unit of work, and it spits out the product of said input - or maybe a compiler errror lol. But that's about it. Configuration and preferences can be overriden with command-line parameters or environment variables. **(Not yet but very soon)** Adding new language providers will be a simple matter of implementing a common interface which can then be automatically loaded and invoked.

Then, you can simply find (or develop) your preferred UI for Headless. A means of sending it work and visualising the output in a way that is most convinient for you or maybe integrates more tightly with your preferred language/SDK. If you really want, you can input code directly into Headless from your terminal... I mean yea you _can_.

## Getting Started

**NOTE: A lot of these details will probably change soon because I will be tidying up this MVP. So, I think there is no point in filling out this section until the 'proper' architecture is estblished**

### Dependencies

* Describe any prerequisites, libraries, OS version, etc., needed before installing program.
* ex. Windows 10

### Installing

* How/where to download your program
* Any modifications needed to be made to files/folders

### Executing program

* How to run the program
* Step-by-step bullets
```
code blocks for commands
```

## Help

**NOTE: Scroll up roughly 20 lines**

Any advise for common problems or issues.
```
command to run if program contains helper info
```

## Version History

* 0.1
  * MVP - This is the absolute bare minimum. A few TODOs are already in the code but I have much more features & improvements planned to implement very soon

## Acknowledgments

This project (and related repos) were inspired heavily by [LinqPad](https://www.linqpad.net/) [<img src="https://github.com/pwalkerdev/Headless/assets/73733025/e4016858-7f66-4195-b127-bcecde74c3a6" width="30" height="30">](https://www.linqpad.net/). LinqPad is a fantastic tool for .NET developers and there is no substitute for it. The idea of this repo is to branch out on the idea of having a 'playground' for writing simple scripts or designing protoypes; without the usual setup/project/boilerplate/etc. But rather than having a specialised tool for C#, I intend to create a more general tool which supports multiple languages. This repo houses only the core components for managing code snippets - kinda like a back-end. With other repos to house the 'front-end(s)'

## License

This project is licensed under the GNU GPLv3 License - see the LICENSE.md file for details
  
---
  
<p align="center">
  Written by Paul Walker - github.com/pwalkerdev
</p>
