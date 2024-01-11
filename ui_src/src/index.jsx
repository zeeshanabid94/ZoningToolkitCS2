import React from 'react';
import ZoningToolkitPanel from './zoning-toolkit-panel';
import Draggable from 'react-draggable';

const ZoningToolkitUI = () => {
    const style = {
        position: "absolute",
        top: 100,
        right: 100,
        color: "white",
        backgroundColor: "rgba(38, 56, 65, 1)", // Light gray with 100% opacity
        borderRadius: "10px", // Rounded edges
        border: "none", // Removing any border or outline
    }
    return <Draggable grid={[50, 50]}>
        <div id="UI" style={style}>
            <ZoningToolkitPanel />
        </div>
    </Draggable>
}

window._$hookui.registerPanel({
    id: "zoning.adjuster",
    name: "Zoning Adjuster",
    icon: "Media/Game/Icons/Zones.svg",
    component: ZoningToolkitUI
})