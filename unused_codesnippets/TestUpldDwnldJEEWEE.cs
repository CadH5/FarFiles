    protected async Task TestUpldDwnldJEEWEE()
    {
        var content = new MultipartFormDataContent();
        using (var client = new HttpClient())
        {
            byte[] bytes = Encoding.UTF8.GetBytes("TestJEEWEE");
            var byteContent = new ByteArrayContent(bytes, 0, bytes.Length);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                        "application/octet-stream");
            content.Add(byteContent, "filesToUpload[]", "JEEWEETest.bin");
            //JEEWEE
            //content.Add(new StringContent("Yes"), "chkOverwrite");

            HttpResponseMessage response = await client.PostAsync(
                        "http://localhost:8080/farfiles/upld.php", content);
            if (!response.IsSuccessStatusCode)
            {
                string responseText = await response.Content.ReadAsStringAsync();
                //throw new Exception(
                //    $"Could not upload 'JEEWEETest.bin' (status: {response.StatusCode}): {responseText}");
            }
        }

        using (var client = new HttpClient())
        {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(MauiProgram.Settings.TimeoutSecsClient)); // total timeout
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    byte[] retBytes = await client.GetByteArrayAsync(
                            "http://localhost:8080/farfiles/dwnld.php?filenameext=JEEWEETest.bin");
                    if (retBytes != null && retBytes.Length > 0)
                    {
                        File.WriteAllBytes(@"C:\temp\jan.bin", retBytes);
                        break;
                    }
                }
                catch (Exception exc)
                {
                }
                await Task.Delay(1 * 1000);
            }
        }
    }

