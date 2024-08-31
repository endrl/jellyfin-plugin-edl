# Jellyfin Plugin EDL

Jellyfin .edl file creation plugin for Kodi and other compatible players. See [Kodi Wiki](https://kodi.wiki/view/Edit_decision_list). Converts MediaSegments based on plugin settings.

## Requirements

- ⚠️ Jellyfin 10.10 unstable
- A writeable media library! You can't use this plugin with read only media libraries!

## Installation instructions

1. Add plugin repository to your server: `https://raw.githubusercontent.com/endrl/jellyfin-plugin-repo/master/manifest.json`
2. Install the EDL Creator plugin from the General section
3. Restart Jellyfin
4. Go to Dashboard -> Scheduled Tasks -> Create EDL and click the play button
5. There is no Task Timer configured, create one if you want to scan daily

### Debug Logging

Change your logging.json file to output debug logs for `Jellyfin.Plugin.Edl`. Make sure to add a comma to the end of `"System": "Warning"`

```jsonc
{
    "Serilog": {
        "MinimumLevel": {
            "Default": "Information",
            "Override": {
                "Microsoft": "Warning",
                "System": "Warning",
                "Jellyfin.Plugin.Edl": "Debug"
            }
        }
       // other stuff
    }
}
```
