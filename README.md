# Un🦆 Blazor intellisense

## Purpose & Features
This extension allows you to define certain global stylesheets (like Bootstrap),
which are expected to be available in all Blazor components.

This is useful when your stylesheets are in a different project than your components
(which VS doesn't recognize)

### Features:
<b>Scopes</b>
- [x] Global stylesheets
- [ ] Scoped stylesheets

<b>HTML</b>
- [x] .razor, .html support
- [x] class="*" support
- [ ] id="*" support 
- [ ] data-* support 

<b>CSS</b>
- [ ] .css support
- [ ] --variables support

<b>Config</b>
- [x] editable .json.user file for .sln
- [ ] User friendly configuration

## Configuration
### Config file
When a solution is loaded, if there is a .css file in the solution (like app.css) [but not like .razor.css]
the extension will create a `BlazorIntellisenseExtensionSettings.json.user` file in the solution directory.

The structure of the file is as follows:
```json
{
  "WhitelistGlobalStylesheetRelativePaths": [
    // Individual stylesheet files
    // These paths are relative to the solution directory

    "Some\\Relative\\Path\\ToMy\\Stylesheet.css",
    "Some\\Relative\\Path\\ToMy\\Stylesheet2.css",
  ],
  "WhitelistGlobalStylesheetDirectoryRelativePaths": [
    // Directories containing stylesheets
    // Also relative to the solution directory

	"src\\ExampleProject.Client\\wwwroot"
  ]
}
```

### Commands
Everything is under "Extensions" tab