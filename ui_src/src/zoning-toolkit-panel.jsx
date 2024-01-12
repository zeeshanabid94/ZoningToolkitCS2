import React from 'react';
import Draggable from 'react-draggable';

class ZoningToolkitPanel extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            zoningMode: '',
            isFocused: false,
            isVisible: false,
            isEnabled: false,
            isUpgradeEnabled: false
        };
    }

    componentDidMount() {
        this.unsub = updateEventFromCSharp('zoning_adjuster_ui_namespace.zoning_mode', (zoningMode) => {
            console.log(`Zoning mode fetched ${zoningMode}`);
            this.setState({ zoningMode: zoningMode})
        })
        this.unsub_upgrade_enabled = updateEventFromCSharp('zoning_adjuster_ui_namespace.upgrade_enabled', (upgradeEnabled) => {
            console.log(`Upgrade Enabled Toggled ${upgradeEnabled}`);
            this.setState({ isUpgradeEnabled: upgradeEnabled})
        })
        this.unsub_visible = updateEventFromCSharp('zoning_adjuster_ui_namespace.visible', (visible) => {
            console.log(`UI visibility changed to ${visible}`);
            this.setState({ isVisible: visible })
        })
        this.setState({ isVisible: true })
    }

    componentWillUnmount() {
        this.unsub();
        this.unsub_upgrade_enabled();
        this.unsub_visible();
    }

    selectZoningMode = (zoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode);
    }

    upgradeEnabledButtonClicked = () => {
        console.log(`Button clicked. Upgrade Enabled ${this.state.isUpgradeEnabled}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'upgrade_enabled', !this.state.isUpgradeEnabled);
    }

    renderZoningModeButton(zoningMode, style) {
        return (
            <button 
                style={style}
                onClick={() => this.selectZoningMode(zoningMode)}
                id={zoningMode}
            >
            {zoningMode}
            </button>
        );
    }

    renderButton(buttonLabel, buttonStyle, onClick) {
        return (
            <button 
                style={buttonStyle}
                onClick={onClick}
                id={buttonLabel}
            >
            {buttonLabel}
            </button>
        );
    }


    render() {
        // Define the styles
        const windowStyle = {
            position: "absolute",
            top: 100,
            right: 100,
            color: "white",
            backgroundColor: "rgba(38, 56, 65, 1)", // Light gray with 100% opacity
            borderRadius: "10px", // Rounded edges
            border: "none", // Removing any border or outline
            padding: '20px',
            width: 'auto',
            margin: '15px auto',
            textAlign: 'center',
            transition: 'box-shadow 0.3s ease-in-out',
            pointerEvents: 'auto',
            display: this.state.isVisible === true ? 'block' : 'none'
        };

        const buttonStyle = {
            margin: '5px',
            padding: '10px 20px',
        };

        const leftButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === 'Left' ? 'green' : 'gray'
        }

        const rightButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === 'Right' ? 'green' : 'gray'
        }
                
        const defaultButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === 'Default' ? 'green' : 'gray',
        }

        const noneButtonStyle = {
            ...buttonStyle,
            background: this.state.zoningMode === 'None' ? 'green' : 'gray',
        }
        
        const enabledButtonStyle = {
            ...buttonStyle,
            background: this.state.isEnabled === true ? 'green' : 'gray',
        }

        
        const upgradeEnabledStyle = {
            ...buttonStyle,
            background: this.state.isUpgradeEnabled === true ? 'green' : 'gray',
        }

        const closeButtonStyle = {
            position: 'absolute', 
            top: '10px', 
            right: '10px', 
            cursor: 'pointer' 
        }
        
        const columnStyle = {
            display: 'flex',
            flexDirection: 'row'
        }

        const { isVisible } = this.state;

        // Apply the styles to the elements
        return (
            <Draggable grid={[50, 50]} id="ZoningToolkitPanel">
                <div 
                    style={windowStyle}
                    id="inner-div"
                >
                    <div id="button-list">
                        {this.renderZoningModeButton("Left", leftButtonStyle)}
                        {this.renderZoningModeButton("Right", rightButtonStyle)}
                        {this.renderZoningModeButton("Default", defaultButtonStyle)}
                        {this.renderZoningModeButton("None", noneButtonStyle)}
                    </div>
                </div>
            </Draggable>
        );
    }
}

function updateEventFromCSharp(event, callback) {
    console.log("Subscribing to update events from game. Event" + event);
    const updateEvent = event + ".update"
    const subscribeEvent = event + ".subscribe"
    const unsubscribeEvent = event + ".unsubscribe"

    var sub = engine.on(updateEvent, callback)
    engine.trigger(subscribeEvent)
    return () => {
        engine.trigger(unsubscribeEvent)
        sub.clear();
    };
}

function sendDataToCSharp(namespace, event, newValue) {
    console.log("Event triggered.");
    engine.trigger(namespace + "." + event, newValue);
}

export default ZoningToolkitPanel;