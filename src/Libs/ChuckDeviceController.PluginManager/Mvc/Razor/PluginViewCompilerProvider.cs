﻿namespace ChuckDeviceController.PluginManager.Mvc.Razor;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

public class PluginViewCompilerProvider : IViewCompilerProvider
{
    private readonly ApplicationPartManager _applicationPartManager;
    private readonly PluginViewCompiler _compiler;

    public PluginViewCompilerProvider(ApplicationPartManager applicationPartManager)
    {
        _applicationPartManager = applicationPartManager;

        var feature = new ViewsFeature();
        _applicationPartManager.PopulateFeature(feature);
        _compiler = new PluginViewCompiler(feature.ViewDescriptors);
    }

    public IViewCompiler GetCompiler() => _compiler;
}