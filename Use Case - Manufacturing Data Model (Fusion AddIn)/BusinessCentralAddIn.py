from .lib import fusionAddInUtils as futil
import adsk.core, traceback
import os

app = adsk.core.Application.get()
ui = app.userInterface

URL = "http://localhost:8080"

CMD_NAME = 'COOLORANGE - Bill of Materials Transfer'
CMD_ID = f'cO_{CMD_NAME}'
CMD_Description = 'Item and BOM Transfer from Fusion to Business Central'
IS_PROMOTED = False

WORKSPACE_ID = "FusionSolidEnvironment"
TAB_ID = "ManageTab"
TAB_NAME = "Manage"

PANEL_NAME = "ERP BOM"
PANEL_ID = f'cO_{PANEL_NAME}'
PANEL_AFTER = "BomPanel"

ICON_FOLDER = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'resources', '')

# Holds references to event handlers
local_handlers = []

# Function to be called when a user clicks the corresponding button in the UI.
def command_created(args: adsk.core.CommandCreatedEventArgs):
    try:
        futil.log(f'{CMD_NAME} Command Created Event')

        futil.add_handler(args.command.incomingFromHTML, browser_incoming, local_handlers=local_handlers)
        futil.add_handler(args.command.destroy, command_destroy, local_handlers=local_handlers)
        futil.add_handler(args.command.navigatingURL, browser_navigating, local_handlers=local_handlers)
        futil.add_handler(args.command.execute, command_execute, local_handlers=local_handlers)

        args.command.isOKButtonVisible = False

        dataFile = app.activeDocument.dataFile
        if (dataFile is None):
            ui.messageBox('The Fusion File is not saved yet. Please save the file and try again.')
            return

        url = f'{URL}?project={dataFile.parentFolder.parentProject.id}&item={dataFile.id}'

        inputs = args.command.commandInputs
        browser_input = inputs.addBrowserCommandInput('browser_input', 'Browser Input', url, 800)
        browser_input.isFullWidth = True

        #inputs.addStringValueInput('name', 'Name', root.name)
        #inputs.addStringValueInput('id', 'Item ID', dataFile.id)
        #inputs.addStringValueInput('project', 'Project ID', dataFile.parentFolder.parentProject.id)
        #inputs.addStringValueInput('hub', 'Hub ID', dataFile.parentProject.parentHub.id)

    except:
        futil.handle_error('command_created')
        ui.messageBox('Failed:\n{}'.format(traceback.format_exc()))

# Event handler for the execute event.
def command_execute(args: adsk.core.CommandEventArgs):
    #ui.messageBox(args)
    args.isValid = True

# Use this to handle events sent from javascript in your palette.
def browser_incoming(args: adsk.core.HTMLEventArgs):
    #ui.messageBox(args)
    args.isValid = True

# Event handler for the navigatingURL event.
def browser_navigating(args: adsk.core.NavigationEventArgs):
    #ui.messageBox(args)
    args.isValid = True

# Event handler for the execute event
def run(context):
    try:
        # Create the command definition
        cmd_def = ui.commandDefinitions.addButtonDefinition(CMD_ID, CMD_NAME, CMD_Description, ICON_FOLDER)
        futil.add_handler(cmd_def.commandCreated, command_created)

        # Workspace
        workspace = ui.workspaces.itemById(WORKSPACE_ID)
        
        # Toolbar
        toolbar_tab = workspace.toolbarTabs.itemById(TAB_ID)
        if toolbar_tab is None:
            toolbar_tab = workspace.toolbarTabs.add(TAB_ID, TAB_NAME)
            ui.messageBox('Tab created!')

        # Panel
        panel = toolbar_tab.toolbarPanels.itemById(PANEL_ID)
        if panel is None:
            panel = toolbar_tab.toolbarPanels.add(PANEL_ID, PANEL_NAME, PANEL_AFTER, False)

        # Create the command control, i.e. a button in the UI.
        control = panel.controls.addCommand(cmd_def)

        # Now you can set various options on the control such as promoting it to always be shown.
        control.isPromoted = True

    except:
        futil.handle_error('run')
        ui.messageBox('Failed:\n{}'.format(traceback.format_exc()))

# Event handler for the stop event.
def stop(context):
    try:
        # Remove all of the event handlers your app has created
        futil.clear_handlers()

        # Get the various UI elements for this command
        workspace = ui.workspaces.itemById(WORKSPACE_ID)
        panel = workspace.toolbarPanels.itemById(PANEL_ID)
        toolbar_tab = workspace.toolbarTabs.itemById(TAB_ID)
        command_control = panel.controls.itemById(CMD_ID)
        command_definition = ui.commandDefinitions.itemById(CMD_ID)

        # Delete the button command control
        if command_control:
            command_control.deleteMe()

        # Delete the command definition
        if command_definition:
            command_definition.deleteMe()

        # Delete the panel if it is empty
        if panel.controls.count == 0:
            panel.deleteMe()

        # Delete the tab if it is empty
        if toolbar_tab.toolbarPanels.count == 0:
            toolbar_tab.deleteMe()

    except:
        futil.handle_error('stop')
        ui.messageBox('Failed:\n{}'.format(traceback.format_exc()))

# This function will be called when the user completes the command.
def command_destroy(args: adsk.core.CommandEventArgs):
    global local_handlers
    local_handlers = []
    futil.log(f'{CMD_NAME} Command Destroy Event')        