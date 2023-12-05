import React from 'react';

const HelloWorld = () => {
    const style = {
        position: "absolute",
        top: 100,
        left: 100,
        color: "white"
    }
    return <div style={style}>
        Hello World
    </div>
}

window._$hookui.registerPanel({
    id: "zoning-adjuster",
    name: "Zoning Adjuster",
    icon: "Media/Game/Icons/Trash.svg",
    component: HelloWorld
})