using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;

namespace Organista
{
    public interface DirectoryEvents
    {
        event EventHandler DirectoryAppeared;
        event EventHandler Directorydisappeared;
        
    }

    public class DirecoryEventArgs : EventArgs
    {
        public string path;
    }
    
    
    public class DirectoryWatcher : DirectoryEvents
    {
        public event EventHandler DirectoryAppeared;
        public event EventHandler Directorydisappeared;

        private string _path;
        
        public DirectoryWatcher(string path)
        {
            _path = path;
            
        }

        private Thread x;
        public void start()
        {
            if (x == null)
            {
                x = new Thread(run);
            }

            running = true;
            x.Start();
        }

        private bool running = true;
        public void stop()
        {
            x.Interrupt();
            running = false;
        }
        public void run()
        {
            string[] usbStorages = new string[0];

            while (running)
            {
                string[] discovered =  Directory.GetDirectories(_path);
                List<string> newCollection = new List<string>();

                foreach (var x in usbStorages)
                {
                    if (!isContaining(x, discovered))
                    {
                        OnDirectoryDisapareared(new DirecoryEventArgs(){path = x});
                    }
                    else
                    {
                        newCollection.Add(x);
                    }
                }

                foreach (var x in discovered)
                {
                    if (!isContaining(x, usbStorages))
                    {
                        OnDirectoryAppear(new DirecoryEventArgs()
                        {
                            path =  x,
                        });
                        newCollection.Add(x);
                    }
                }

                usbStorages = newCollection.ToArray();
                
                Thread.Sleep(1000);
            }

            x = null;
        }


        bool isContaining(string path, string[] collection)
        {
            foreach (var x in collection)
            {
                if (x.Contains(path))
                {
                    return true;
                }
            }
            return false;
        }
        
        
        protected virtual async void OnDirectoryAppear(DirecoryEventArgs e)
        {
            try
            {
                DirectoryAppeared?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                
            }
        }
        
        protected virtual async void OnDirectoryDisapareared(DirecoryEventArgs e)
        {
            try
            {
                Directorydisappeared?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}