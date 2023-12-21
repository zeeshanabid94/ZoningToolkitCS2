import React from 'react';

class ZoningAdjusterPanel extends React.Component {
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

    handleClose = () => {
        this.setState({ isVisible: false });
    }

    selectZoningMode = (zoningMode) => {
        console.log(`Button clicked. Zoning mode ${zoningMode}`);
        sendDataToCSharp('zoning_adjuster_ui_namespace', 'zoning_mode_update', zoningMode);
    }

    enabledButtonClicked = () => {
        console.log(`Button clicked. Enabled ${this.state.isEnabled}`);
        sendDataToCSharp(('zoning_adjuster_ui_namespace', 'apply_to_new_roads', !this.state.isEnabled));
    }

    upgradeEnabledButtonClicked = () => {
        console.log(`Button clicked. Upgrade Enabled ${this.state.isUpgradeEnabled}`);
        sendDataToCSharp(('zoning_adjuster_ui_namespace', 'upgrade_enabled', !this.state.isUpgradeEnabled));
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
            border: '1px solid #ccc',
            padding: '20px',
            width: '250px',
            margin: '20px auto',
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
            background: 'linear-gradient(to right, white 55%, gray 45%)',
            border: this.state.zoningMode === 'Left' ? '10px solid green' : 'none',
        }

        const rightButtonStyle = {
            ...buttonStyle,
            background: 'linear-gradient(to right, gray 55%, white 55%)',
            border: this.state.zoningMode === 'Right' ? '10px solid green' : 'none',
        }
                
        const defaultButtonStyle = {
            ...buttonStyle,
            background: 'linear-gradient(to right, white 20%, gray 30%, gray 70%, white 80%)',
            border: this.state.zoningMode === 'Default' ? '10px solid green' : 'none',
        }

        const noneButtonStyle = {
            ...buttonStyle,
            background: 'gray',
            border: this.state.zoningMode === 'None' ? '10px solid green' : 'none',
        }
        
        const enabledButtonStyle = {
            ...buttonStyle,
            background: 'gray',
            border: this.state.isEnabled === true ? '10px solid green' : 'none',
        }

        
        const upgradeEnabledStyle = {
            ...buttonStyle,
            background: 'gray',
            border: this.state.isUpgradeEnabled === true ? '10px solid green' : 'none',
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
            <>
                {isVisible && (
                    <div 
                        style={windowStyle}
                    >
                        <button 
                            style={closeButtonStyle}
                            onClick={this.handleClose} // Assuming you have a method to handle the close action
                        >
                            X
                        </button>
                        <div style={columnStyle}>
                            <div style={{ flex: 1}}>
                                {this.renderZoningModeButton("Left", leftButtonStyle)}
                                {this.renderZoningModeButton("Right", rightButtonStyle)}
                                {this.renderZoningModeButton("Default", defaultButtonStyle)}
                                {this.renderZoningModeButton("None", noneButtonStyle)}
                            </div>

                            <div style={{ flex: 1}}>
                                {this.renderButton("Enabled", enabledButtonStyle, this.enabledButtonClicked)}
                                {this.renderButton("UpgradeEnabled", upgradeEnabledStyle, this.upgradeEnabledButtonClicked)}
                            </div>
                        </div>
                    </div>
                )}
            </>
            
        );
    }

    get_zoning_mode() {
        console.log("Getting zoning mode.");

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

export default ZoningAdjusterPanel;