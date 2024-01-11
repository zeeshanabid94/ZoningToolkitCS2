import React from 'react';

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
        this.unsub_enabled = updateEventFromCSharp('zoning_adjuster_ui_namespace.apply_to_new_roads', (apply_to_new_roads) => {
            console.log(`Enabled Toggled ${apply_to_new_roads}`);
            this.setState({ isEnabled: apply_to_new_roads})
        })
        this.unsub_upgrade_enabled = updateEventFromCSharp('zoning_adjuster_ui_namespace.upgrade_enabled', (upgradeEnabled) => {
            console.log(`Upgrade Enabled Toggled ${upgradeEnabled}`);
            this.setState({ isUpgradeEnabled: upgradeEnabled})
        })
        this.setState({ isVisible: true })
    }

    componentWillUnmount() {
        this.unsub();
        this.unsub_enabled();
        this.unsub_upgrade_enabled();
    }

    selectZoningMode = (zoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode);
    }

    enabledButtonClicked = () => {
        console.log(`Button clicked. Enabled ${this.state.isEnabled}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'apply_to_new_roads', !this.state.isEnabled);
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
            >
            {buttonLabel}
            </button>
        );
    }


    render() {
        // Define the styles
        const windowStyle = {
            border: 'none',
            padding: '20px',
            width: 'auto',
            margin: '15px auto',
            textAlign: 'center',
            transition: 'box-shadow 0.3s ease-in-out',
            pointerEvents: 'auto'
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
            <div 
                style={windowStyle}
            >
                <div>
                    {this.renderZoningModeButton("Left", leftButtonStyle)}
                    {this.renderZoningModeButton("Right", rightButtonStyle)}
                    {this.renderZoningModeButton("Default", defaultButtonStyle)}
                    {this.renderZoningModeButton("None", noneButtonStyle)}
                </div>
            </div>
        );
    }
}

function updateEventFromCSharp(event, callback) {
    console.log("Updating.");
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