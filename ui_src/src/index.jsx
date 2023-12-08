import React from 'react';
import ZoningAdjusterPanel from './zoning-adjuster-panel';

const ZoningAdjusterUI = () => {
    const style = {
        position: "absolute",
        top: 100,
        left: 100,
        color: "white",
        backgroundColor: "rgba(173, 216, 230, 0.75)", // Light blue with 75% opacity
        borderRadius: "10px", // Rounded edges
        border: "none", // Removing any border or outline
    }
    return <div id="UI" style={style}>
        <Text>Zoning Adjuster</Text>
        <ZoningAdjusterPanel />
    </div>
}

window._$hookui.registerPanel({
    id: "zoning.adjuster",
    name: "Zoning Adjuster",
    icon: "Media/Game/Icons/Zones.svg",
    component: ZoningAdjusterUI
})