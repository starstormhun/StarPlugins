# StarPlugins

Various plugins for Illusion's Koikatsu / Koikatsu Sunshine games.
Configuration Manager is recommended to make changing the settings from these plugins easier.

## How to install
1. Install the latest build of [BepInEx](https://github.com/BepInEx/BepInEx/releases)
2. Download the latest release for your game from [the releases page](../../releases)
3. Plugin dlls go to `BepInEx/plugins` (or a subfolder thereof)

## Plugins

#### Axis Unlocker [KK/KKS]
Makes the manipulation axis speed and size sliders' minimum and maximum values configurable.
Optionally converts the sliders to logarithmic base.

#### Expression Control [KK/KKS]
This is an old plugin which I decompiled and modified to also be able to be used on males.

#### Shader Fixer [KK/KKS]
By default it fixes the KKUSS shaders when used in Koikatsu Sunshine by making sure they always have at least a flat normal map.
The shader filter is configurable so it can be extended to work on other shaders.

#### Light Settings [KK/KKS]
Allows changing more properties of lights than available by default.
Also allows the usage of light cookies, editing of map lights, and toggling of the chara light.

#### Light Toggler [KK/KKS]
Automatically toggles on/off lights when a parent folder or object is toggled on/off.
Also works for items that have built-in lights.
The changes persist through saves/loads without adding extended data to the scene.

#### Mass Shader Editor [KK/KKS]
A plugin that lets you edit the properties of multiple shaders at once, based on item selection.
For both Maker and Studio. Also lets you swap shaders for others.
Contains a built-in tutorial and help section due to its many options.
