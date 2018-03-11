---
layout: default
---

### [](#header-3) Introduction

EasyDiscordBot is a Discord Bot written in C# for the Haruhi Discord. It can be used on any Server you like and is easy to configure.

#### [](#header-4)Features

EasyDiscordBot offers multiple active commands, detailed below, all of them are prefixed with: 
```
SOS!
```

| command | parameter            | function                                                 |
|:--------|:---------------------|:---------------------------------------------------------|
| help    |                      | Prints a help message.                                   |
| roles   |                      | Prints the list of settable/allowed roles.               |
| role    | rolename, no # or id | Set or removes the role (if allowed) from the user.      |
| addrole | rolename, no # or id | Adds a role to the list of settable/allowed roles.       |
| delrole | rolename, no # or id | Removes a role from the list of settable/allowed roles.  |


EasyDiscordBot also offers multiple passive commands, detailed below, all of them are in reaction to user messages:

| message contains          | action                                                                                                       |
|:--------------------------|:-------------------------------------------------------------------------------------------------------------|
| word from replacementlist | Bot reacts with a snarky message, "correction", and replaces the word from replacementlist with a better one |  

![Example Correction](https://raw.githubusercontent.com/master117/EasyDiscordBot/master/docs/example1.png)

Notes

* All commands are unaffected by capitalization. 
* The settable rolelist is stored and restored after restart.
* Some commands have spam protection.
* Have Fun!