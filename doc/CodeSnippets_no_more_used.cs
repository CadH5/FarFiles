            //JEEWEE: INTERESTING CODE
            //===============================================
            if (false)
            {
                var stunData = await GetPublicIPAsync();
                await Shell.Current.DisplayAlert("GetPublicIPAsync",
                    $"publicIP={stunData.publicIP}, natType={stunData.natType}", "Cancel");
            }

            if (false)
            {
                msg = await TestStunUdpConnection();
                await Shell.Current.DisplayAlert("Test", msg, "Cancel");
            }
            //===============================================



    //JEEWEE: Interesting code:
    public async Task<(IPAddress publicIP, string natType)> GetPublicIPAsync()
    {
        var stunServer = "stun.l.google.com";
        var stunPort = 19302;

        using var udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        udpClient.Connect(stunServer, stunPort);

        var localEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint!;

        // Resolve STUN server hostname to an IP address
        var addresses = await Dns.GetHostAddressesAsync(stunServer);
        var stunServerIP = addresses
            .First(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        var stunServerEndPoint = new IPEndPoint(stunServerIP, stunPort);

        // Create StunClient3489 instance
        var stunClient = new StunClient3489(localEndPoint, stunServerEndPoint);
        //var stunClient = new StunClient5389UDP(localEndPoint, stunServerEndPoint);

        await stunClient.QueryAsync(); // Perform the STUN request

        if (stunClient.State.PublicEndPoint == null)
        {
            throw new Exception("Failed to determine public IP");
        }

        //StunClient3489:
        return (stunClient.State.PublicEndPoint.Address, stunClient.State.NatType.ToString());

        //StunClient5389UDP:
        //return (stunClient.State.PublicEndPoint.Address, stunClient.State.MappingBehavior.ToString());
    }
