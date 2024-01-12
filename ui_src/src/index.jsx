import React from 'react';
import ZoningToolkitPanel from './zoning-toolkit-panel';

window._$hookui.registerPanel({
    id: "zoning.toolkit",
    name: "Zoning Toolkit",
    icon: "Media/Game/Icons/Zones.svg",
    component: ZoningToolkitPanel
})