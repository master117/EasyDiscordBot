---
layout: default
---
<h3>Introduction</h3>
EasyDiscordBot is a Discord Bot written in C# for the Haruhi Discord. It can be used on any Server you like and is easy to configure.

<h4>Features</h4>
EasyDiscordBot offers multiple functions, detailed below, all of them are prefixed with
<div class="highlighter-rouge">
    <div class="highlight">
        <pre class="highlight">
        <code>
                SOS!
            </code>
        </pre>
    </div>
</div>

<table>
    <thead>
        <tr>
            <th style="text-align: left">command</th>
            <th style="text-align: left">parameters</th>
            <th style="text-align: left">function</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td style="text-align: left">
                help
            </td>
            <td style="text-align: left"></td>
            <td style="text-align: left">
                Prints a help message.
            </td>
        </tr>
        <tr>
            <td style="text-align: left">
                roles
            </td>
            <td style="text-align: left"></td>
            <td style="text-align: left">
                Prints the list of settable/allowed roles.
            </td>
        </tr>
        <tr>
            <td style="text-align: left">
                role
            </td>
            <td style="text-align: left">
                a single rolename, without # or id
            </td>
            <td style="text-align: left">
                Set or removes the role (if allowed) from the user.
            </td>
        </tr>
        <tr>
            <td style="text-align: left">
                addrole
            </td>
            <td style="text-align: left">
                a single rolename, without # or id
            </td>
            <td style="text-align: left">
                Adds a role to the list of settable/allowed roles.
            </td>
        </tr>
        <tr>
            <td style="text-align: left">
                delrole
            </td>
            <td style="text-align: left">
                a single rolename, without # or id
            </td>
            <td style="text-align: left">
                Removes a role from the list of settable/allowed roles.
            </td>
        </tr>
    </tbody>
</table>