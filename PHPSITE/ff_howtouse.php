<?php
$C5pth = '..';
require($C5pth . '/header.inc');
WriteLogPageOpened($C5pth, "FarFiles ff_howtouse.php");
?>

    <title>How to use FarFiles</title>
    <body>

    <div class="cols-container">

        <?php
        require('leftmenu_farfiles.inc');
        ?>

        <div class="cols-item-middle60">

            <h1>How to use FarFiles</h1>

            <ul>
                <li><a href="#genproc">General procedure</a></li>
                <li><a href="#connstunerr">Connect: error obtaining udp port from stun server</a></li>
                <li><a href="#connkey">Connection key</a></li>
                <li><a href="#connmodes">Connect modes</a></li>
                <li><a href="#commmodes">Communication modes</a></li>
                <li><a href="#states">States</a></li>
                <li><a href="#clientnav">Client navigator</a></li>
                <li><a href="#morebutt">More buttons in Client navigator</a></li>
                <li><a href="#clmainpage">Client Page and Main Page</a></li>
                <li><a href="#aboutpage">About Page</a></li>
                <li><a href="#closebutt">Button 'Close App' or 'X'</a></li>
            </ul>

            <a id="genproc"></a>
            <h2>General procedure</h2>

            <p>
                Two instances of the app connect to each other, using the same "connect key".
                One of them must be the "Server", the other the "Client". First, the "Server" must
                connect (to our central server that is the intermedium), then the "Client".
                Subsequently, the "Client" can copy files and folders from the "Server".<br>
                The users need to have some conversation outside of this app, in order to for example
                enter the same connect key.
            </p>
            <p>
                Procedure for user "Server":
            </p>
            <ol>
                <li>Browses locally to determine the root path that is exposed to the "Client"</li>
                <li>Enters a connect key.</li>
                <li>Chooses the communication mode, out of three options.</li>
                <li>Connects to our central server ("registers").</li>
                <li>Waits for the "Client".</li>
            </ol>
            <img src="images/ff_procedure_connect_2.gif">
            <p>
                Procedure for user "Client":
            </p>
            <ol>
                <li>Browses locally to determine the root path where files
                and folders will be copied to.</li>
                <li>Enters same connect key as the "Server".</li>
                <li>Chooses same communication mode as the "Server".</li>
                <li>Connects to our central server ("registers").</li>
                <li>If all is right, sees appearing files and folders on "Server"
                and can start copying.</li>
            </ol>
            <p>
                Root path, connect key, communication mode will be remembered at next app start
                and used as the defaults.
            </p>

            <br>
            <br>
            <a id="connstunerr"></a>
            <h2>Connect: error obtaining udp port from stun server</h2>
            <img src="images/ff_errstunserver.jpg">
            <p>
                Every now and then this error may come up after pressing the button "Connect". Mostly, the remedy is
                to try again. (If there isn't a real issue with the Stun server; see also Advanced).
                XXXXXXXXXXXXXXXXXX JEEWEE
            </p>

            <br>
            <br>
            <a id="connkey"></a>
            <h2>Connection key</h2>
            <img src="images/connectionkey.bmp">
            <p>
                The "Connection key" does not have to be a strong password or so. It just serves as a temporary
                identification of the connection between "Server" and "Client", and must be unique at the
                moment of connecting the "Server". If not, an error message will be displayed ("ConnectKey already occupied
                for server") and you´ll have to invent another one.<br>
                <br>
                Sometimes, because of inappropriate terminating of the app so that "unregistering" is skipped,
                a ConnectKey remains unavailable. In that case, after a day it will be released by our central server.
            </p>

            <br>
            <br>
            <a id="connmodes"></a>
            <h2>Connect modes</h2>
            <img src="images/ff_connectmodes.gif">
            <ul>
                <li><b>Client:</b> does the active work of selecting and copying.</li>
                <li><b>Server:</b> exposes files and folders to be copied to the client;
                    is listening whether messages from Client are coming in.</li>
                <li><b>Server, writable:</b> same, but client can also copy files and folders
                    TO this server.</li>
            </ul>

            <br>
            <br>
            <a id="commmodes"></a>
            <h2>Communication modes</h2>
            <img src="images/communication_mode.jpg">
            <br>
            <h3>Role central server</h3>
            <p>
                "Server" instance as well as "Client" instance register themselves in our central server to get
                details of the connection to the other instance. First. the "Server" must register, then the
                "Client".
            </p>

            <br>
            <h3>Local IP</h3>
            <p>
                If "Server" and "Client" instances are within the same LAN (let's say using the same router), connection through Local IP
                is probably the best communication mode. Communication goes by UDP protocol.
            </p>

            <br>
            <h3>NAT Hole Punching</h3>
            <p>
                NAT Hole Punching may provide a communication by UDP protocol if "Server" and "Client" instances
                are not within the same LAN. However, probably (depending on the 'NAT types' of the routers)
                connection will fail (you will get an error message after some 20 seconds).
            </p>

            <br>
            <h3>Central Server</h3>
            <p>
                In this scenario our central server is not only used for registration as the intermedium for
                meta data between "Server" and "Client" instance, but also for all communication. Communication
                does not go by UDP. This is the most probably successful one of the modes, but copying
                files and folders goes waaaaaaaaay slower than by UDP protocol directly.
            </p>

            <br>
            <br>
            <a id="states"></a>
            <h2>States</h2>
            <p>
                The round image left above indicates the state of the app:
            </p>
            <ul>
                <li><img src="images/ffstate_unreg.gif">Unregistered: not registered in central server.</li>
                <li><img src="images/ffstate_reg.gif">Registered: registered in central server but
                    not yet connected to other side.</li>
                <li><img src="images/ffstate_conn.gif">Connected: Connected to other side.</li>
                <li><img src="images/ffstate_intrans.gif">In transaction: Connected to other side and
                    in a trsnsaction conversation.</li>
            </ul>

            <br>
            <br>
            <a id="clientnav"></a>
            <h2>Client navigator</h2>
            <img src="images/ff_navigator_2.gif"><br>
            <p>
                The navigating control for the "Client" instance appears when a connection with the
                "Server" is established. It shows folders on the server as yellow
                symbols and files as white ones. The control may work a bit different from similar Windows
                controls you may be familiar with, because of the need for similarity on Android devices.
            </p>

            <br>
            <h3>Selecting and deselecting</h3>
            <p>
                Selecting and deselecting a file and/or folder is done by clicking on it; on Android devices:
                by touching it.<br>
                Multiselecting on Windows can be done by pressing Shift key while selecting. Actually on Android devices
                an equivalent for this is not available; to select ALL files (not folders) use buttons "...",
                "select all files".<br>
                Button "Clear all" is your friend; it deselects all files and folders.<br>
                If exactly one FOLDER is selected and no files, button "goto" is enabled.
            </p>

            <br>
            <h3>clear all</h3>
            <p>
                This button is your friend; it lets deselect all files and folders.<br>
            </p>

            <br>
            <h3>goto</h3>
            <p>
                This button lets request the "Server" instance to move the scope to the one and only selected folder.<br>
            </p>

            <br>
            <h3>copy</h3>
            <p>
                After a confirmation, this button lets start the process of copying the selected file(s) and folders(s)
                with their contents from "Server" to "Client".<br>
            </p>

            <br>
            <h3>existing files:</h3>
            <p>
                With this control you indicate what to do with files already existing in the copy process:
                overwrite or skip.
            </p>

            <br>
            <br>
            <a id="morebutt"></a>
            <h2>More controls in Client navigator</h2>
            <p>
                Button "..." lets appear or disappear more controls.
            </p>

            <br>
            <h3>files: see:</h3>
            <img src="images/ff_files_see.gif">
            <p>
                Choice list allows for seeing, besides the name, either Size or Date of files.
            </p>

            <br>
            <h3>files: order by:</h3>
            <img src="images/ff_files_orderby.gif">
            <p>
                Choice list allows for files being ordered by Name, Size or Date, with a checkbox to specify
                reversed ordering or not. (Note: folders are always ordered by Name).
            </p>

            <br>
            <h3>select all files</h3>
            <img src="images/ff_btnselall.jpg">
            <p>
                This button lets select all files, not folders, in the navigator.
            </p>

            <br>
            <h3>select filtered files</h3>
            <img src="images/ff_btnselfiltered.jpg">
            <p>
                This button lets select all files, not folders, of which the names contain the text string that you
                enter right of the button, case ignored. Example: when you enter .jpg it will select all
                .jpg files.
            </p>

            <br>
            <h3>Swap server/client</h3>
            <img src="images/ff_btnswap_1.jpg">
            <img src="images/ff_btnswap_2.jpg">
            <img src="images/ff_btnswap_3.jpg">
            <br>
            <p>
                This button lets ask the "Server" instance whether it agrees on swapping. This may be a solution
                in cases where connecting succeeds one way, but fails the other way round. So: one
                instance is the "Server" and tries to connect to the "Client" and it fails. But the other way round
                connection succeeds (this happens).<br>
                <br>
                After confirming swap attempt, at "Server" side user must agree (an OK/Cancel dialog comes up).
                If it is agreed, "Client" becomes "Server" and "Server" becomes "Client". Maybe has to use "Back to files >>>>"
                button to get to the Client navigator.
            </p>

            <br>
            <h3>copy TO server (only available if the server is writable)</h3>
            <img src="images/ff_copyfromto.gif">
            <p>
                This button lets switch the copy direction. The navigator will show your local files
                and folders (folders colored blue instead of yellow), and when you copy, you copy TO
                the server instead of from the server.
            </p>

            <br>
            <br>
            <a id="clmainpage"></a>
            <h2>Client Page and Main Page</h2>
            <img src="images/ff_clientpage_back.gif">
            <p>
                From Client Page and navigator, you can go back to the main page using the arrow
                above left. You can also go to the Advanced Page or the About Page, then return to
                Main Page. To return to the Client Page, use button "Back to files &gt;&gt;&gt;&gt;".
            </p>

            <br>
            <br>
            <a id="aboutpage"></a>
            <h2>About Page</h2>
            <img src="images/ff_aboutpage.jpg">
            <p>
                The About page, that opens after you press the button "About ..." in the main page, shows some general
                information and a link that leads to the site that contains this manual. You can go back to the
                main page using the arrow above left.
            </p>

            <br>
            <br>
            <a id="advancedpage"></a>
            <h2>Advanced Page</h2>
            <img src="images/ff_advancedpage.jpg">
            <p>
                The Advanced page, that opens after you press the button "Advanced ..." in the main page, shows
                information that is more interesting after a connection has been established. Also some advanced
                settings can be changed here, although we recommend not to do so.<br>
                Note: settings about the Stun server do not show up if Communication Mode is "Central Server",
                which does not require UDP and no Stun server.
            </p>

            <br>
            <br>
            <a id="closebutt"></a>
            <h2>Button 'Close App' or 'X'</h2>
            <img src="images/ff_btnclose.jpg"><br>
            <p>
                Button "X" closes the app, and unregisters at our central server if it was registered. For this,
                it is the preferred way to close the app, instead of swiping it away on Android. (On
                Windows, it unregisters also if you use the Windows closing method by the X, above right).<br>
                <br>If the app closes without unregistering, there is no big damage, only the connection key
                cannot be used for 24 hours (if a "Server" tries using it, an error message "occupied" results).<br>
                <br>
                Another thing that the app does when you close it by this button, if connected, is trying to inform the other
                side of its shutdown.
            </p>



            <br>
            <br>
            <br>
            <br>

        </div>

    </div>

    <div class="cols-item-side20">
        <div class="horcenter" style="width:90%">
            <img src="images/FarFiles.png">
        </div>
    </div>

    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>
    <BR>


    </body>

<?php
require('../footer.inc');


