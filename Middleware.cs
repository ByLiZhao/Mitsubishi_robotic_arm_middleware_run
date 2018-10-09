using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using System.Threading;

namespace Middleware_Run
{
    public class middleware : IDisposable
    {
        private StaTaskScheduler sta_ { get; set; } = new StaTaskScheduler(1);
        private AxMELFARXMLib.AxMelfaRxM ax_;
        //number of all robots connected, 1-32
        public static int Max_robots_index { set; get; } = 32;
        public static List<string> Robot_list { private set; get; } = new List<string>();

        public int current_robot_index  // between 1-32
        {
            set
            {
                // Check function input
                if ((value < 1) || (value > Max_robots_index))
                {
                    current_robot_index_ = 0;   //meaning to robot
                }
                current_robot_index_ = value;
            }
            get
            {
                return this.current_robot_index_;
            }
        }    //first robot in the list as default, 1-32.
        private int current_robot_index_;
        public int display { private set; get; } = 0;               //don't display progress as default, whether display
                                                                    //text properties
        public string request_ID { private set; get; }                      //Request ID       
        public string cycle_time { private set; get; }                      //Cycle Time        
        public string send_data { set; get; }               //Send data         
        public string robot_name { private set; get; }              //Robot name        
        public string receive_ID { private set; get; }              //Receive ID        
        public string receive_status { private set; get; }          //Receive status   
        public string receive_error { private set; get; }           //Receive error   
        public string receive_data { private set; get; }            //Receive data     
        private string receive_raw_data;
        //read from robot controller
        public List<string> program_list { private set; get; } = new List<string>();
        public List<string> error_list { private set; get; } = new List<string>();
        public string[] robot_current_pose { private set; get; } = new string[8];
        private bool disposed = false;
        public async Task<int> async_init_middleware()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                this.ax_ = new AxMELFARXMLib.AxMelfaRxM();
                ax_.CreateControl();
                ax_.MsgRecvEvent += new System.EventHandler(this.message_receiv_event_handler);
                return 1;
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            sta_);
            return taskResult;

        }

        public async Task<int> async_init_active_core()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                init_text_properties();
                // Start Communication server 
                bool ret = start_communication_server();
                if (ret == false)
                {
                    return -1;
                }
                this.current_robot_index = 1; //first robot in the robot list as default.
                result = init_robots_info();
                return result;
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            sta_);

            return taskResult;
        }

        public async Task<int> async_get_program_list()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.get_robot_program_list();
                return result;
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            sta_);

            return taskResult;
        }

        public async Task<int> async_start(string program_name)
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.start(program_name);
                return result;
            },
            CancellationToken.None,
            TaskCreationOptions.None,
            sta_);

            return taskResult;
        }

        public async Task<int> async_servo_on()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.servo_on();
                return result;
            },
           CancellationToken.None,
           TaskCreationOptions.None,
           sta_);

            return taskResult;
        }

        public async Task<int> async_servo_off()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.servo_off();
                return result;
            },
           CancellationToken.None,
           TaskCreationOptions.None,
           sta_);

            return taskResult;
        }

        public async Task<int> async_reset()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.reset();
                return result;
            },
           CancellationToken.None,
           TaskCreationOptions.None,
           sta_);

            return taskResult;
        }

        public async Task<int> async_reset_program()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.reset_program();
                return result;
            },
           CancellationToken.None,
           TaskCreationOptions.None,
           sta_);

            return taskResult;
        }

        public async Task<int> async_request_cancel()
        {
            var taskResult = await Task.Factory.StartNew(() =>
            {
                int result = 0;
                result = this.request_cancel();
                return result;
            },
           CancellationToken.None,
           TaskCreationOptions.None,
           sta_);

            return taskResult;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (ax_.ServerLive())
                    {
                        ax_.ServerKill();
                    }
                    ax_.Dispose();
                    this.sta_.Dispose();
                }         
                disposed = true;
            }
        }

        ~middleware()
        {
            Dispose(false);
        }

        private void init_text_properties()
        {
            request_ID = "0";               //Request ID
            cycle_time = "0";               //Cycle Time
            send_data = string.Empty;       //Send data

            robot_name = "0";               //Robot name
            receive_ID = "0";               //Receive ID
            receive_status = "0";           //Receive status
            receive_error = "0";            //Receive error
            receive_data = string.Empty;    //Receive data
        }
        //
        private bool start_communication_server()
        {
            bool result = false;
            result = this.ax_.ServerLive();
            // if the communication server is already alive, don't bother to start again.
            if (result)
            {
                return true;
            }
            else
            {
                result = this.ax_.ServerStart();
                Thread.Sleep(1000);
                return result;
            }
        }
        //
        private int init_robots_info()
        {
            int max_robots_index = 0; //index of controllers, starts with 0.
            string data = string.Empty;
            string robot_name_and_ID = string.Empty;
            string merged_info = string.Empty;
            #region
            //amount_of_robots: The number denoting robot controllers set by Communication Server.
            //data: The identifiers and names of robot controllers set by Communication Server.
            //GetRobotComSetting returns true if communication server connects with the robot controller
            //false if not connected
            #endregion
            bool result = this.ax_.GetRoboComSetting(ref max_robots_index, ref data);
            if (result == false)
            {
                return -1;
            }
            Max_robots_index = max_robots_index; // update No_of_robots property.
            robot_name_and_ID = data;
            for (int i = 0; i < max_robots_index; ++i)  // Loop for all robot controllers
            {
                data = string.Empty;
                this.ax_.GetOneDataCPP(i * 2, robot_name_and_ID, ref data);
                merged_info = data;
                data = string.Empty;
                this.ax_.GetOneDataCPP(i * 2 + 1, robot_name_and_ID, ref data);
                merged_info += " : ";
                merged_info += data;
                Robot_list.Add(merged_info);
            }
            return 1;
        }

        // Recveive event processing
        private void message_receiv_event_handler(object sender, EventArgs e)
        {
            int robot_index = 0;
            int receive_ID = 0;
            int status = 0;
            int error = 0;
            string data = string.Empty;
            #region
            //long GetRecvData (long MsgID, String Data, long Status, long Error)
            //[OUT] RobotID, The identifier(1 to 32) of the robot controller is set.
            //[OUT] MsgID, The ID to identify a request is set.For more information about the message ID, 
            //see 5.3, "Request IDs Specified by Request for Service Methods".
            //[OUT] Data, Reception data is set.The contents of data vary depending on the request ID.
            //For more information, see 5.3, "Request IDs Specified by Request for Service Methods".
            //[OUT] Status, The reception status is set. If a number other than 1 (received successfully) was set, 
            //an error message is set in Data. 
            //1 : Received successfully 
            //2 : Transmission error 
            //3 : Reception timeout 
            //4 : Transmission canceled(when a request was canceled by pressing the [Cancel] button of Communication Server 2) 
            //5 : Execution error (execution result was an error) 
            //10 : Undefined request (when an undefined request ID was specified) 
            //11 : Invalid argument for a request (when a request ID and transmission data do not match)
            //[OUT] Error, When "5" is set in Status, an error number of the robot controller is set.
            //[Return value]
            //Returns the reception result.
            //1 : Successful
            //0 : Invalid data
            #endregion
            int result = this.ax_.GetRecvDataM(
                ref robot_index,
                ref receive_ID,
                ref data,
                ref status,
                ref error);
            if (1 != result)
            {
                handle_receive_error();
                return;
            }
            this.robot_name = robot_index.ToString();//update class properties related with message receiving
            this.receive_ID = receive_ID.ToString();
            this.receive_status = status.ToString();
            this.receive_error = error.ToString();
            this.receive_raw_data = string.Copy(data);
            int data_count = 0;
            string temp = string.Empty;
            switch (receive_ID)
            {
                //Get the robot program list
                case 106:
                    #region
                    //Get the error NO
                    //long GetOneDataCPP (long Point, LPCTSTR Data, BSTR* Onedata)
                    //[Arguments]
                    //[IN] Point, Specify the index of the item to be acquired.Index starts with 0.
                    //[IN] Data, Specify the received data acquired by the get received data (GetRecvData)
                    //[OUT] Onedata, The item of the specified index is set.
                    //[Return value]
                    //1 : Successful
                    //0 : Failed
                    #endregion
                    this.ax_.GetOneDataCPP(0, data, ref temp);
                    data_count = int.Parse(temp);
                    for (int i = 0; i < data_count; ++i)
                    {
                        temp = string.Empty;
                        this.ax_.GetOneDataCPP(i * 12 + 1, data, ref temp);
                        string[] temp2 = temp.Split('.');
                        this.program_list.Add(temp2[0]);
                    }
                    break;
                case 203:
                    // Get robot error list
                    this.error_list.Clear();
                    this.ax_.GetOneDataCPP(0, data, ref temp);
                    data_count = int.Parse(temp);
                    for (int i = 0; i < data_count; ++i)
                    {
                        temp = string.Empty;
                        this.ax_.GetOneDataCPP(i * 8 + 1, data, ref temp);
                        // Error No.
                        this.error_list.Add(temp);
                        this.ax_.GetOneDataCPP(i * 8 + 2, data, ref temp);
                        // Error Message
                        this.error_list.Add(temp);
                        this.ax_.GetOneDataCPP(i * 8 + 3, data, ref temp);
                        // Date
                        this.error_list.Add(temp);
                        this.ax_.GetOneDataCPP(i * 8 + 4, data, ref temp);
                        // Time
                        this.error_list.Add(temp);
                    }
                    break;
                case 235:
                    // Get robot current position
                    for (int i = 0; i < 8; ++i)
                    {
                        temp = string.Empty;
                        this.ax_.GetOneDataCPP(i + 16, data, ref temp);
                        robot_current_pose[i] = temp;
                    }
                    break;
                default:
                    data_count = this.ax_.GetDataCnt(data);
                    string sMsg = string.Empty;
                    string sOneData = string.Empty;
                    for (int i = 0; i < data_count; ++i)
                    {
                        sOneData = this.ax_.GetOneData(i, data);
                        sMsg = sMsg + sOneData + "\r\n";
                    }
                    this.receive_data = sMsg;
                    break;
            }
        }
        //
        private void handle_receive_error()
        {
            request_cancel();
            stop();
        }

        //Get program list from current robot controller
        private int get_robot_program_list()
        {//get program list of current robot
         // Read list of robot programs
            this.send_data = string.Empty;
            this.request_service(
                this.current_robot_index,
                106,
                0,
                0);
            // Monitoring the error (interval 1000ms)
            this.send_data = string.Empty;
            this.request_service(
                this.current_robot_index,
                203,
                0,
                1000);
            // Monitoring the position (interval 1000ms)
            this.send_data = string.Format("1\n1\n1");
            int result = this.request_service(
                this.current_robot_index,
                235,
                this.send_data.Length,
                1000);
            return result;
        }

        // Servo on with current robot index
        private int servo_on()
        {
            this.send_data = string.Format("1\n1");
            int result = this.request_service(
                this.current_robot_index,
                403,
                this.send_data.Length,
                0);
            return result;
        }

        // Servo off with current robot index
        private int servo_off()
        {
            this.send_data = string.Format("1\n0");
            int result = this.request_service(
                this.current_robot_index,
                403,
                this.send_data.Length,
                0);
            return result;
        }

        // Start with current robot index and selected program
        private int start(string program_name)
        {
            this.send_data = string.Format("1\n{0}\n1", program_name);
            int result = this.request_service(
                this.current_robot_index,
                400,
                this.send_data.Length,
                0);
            return result;
        }

        // Stop executing program with current robot index
        private int stop()
        {
            this.send_data = string.Format("0");
            int result = this.request_service(
                this.current_robot_index,
                401,
                this.send_data.Length,
                0);
            return result;
        }

        //Error reset with current robot index
        private int reset()
        {
            this.send_data = string.Empty;
            int result = this.request_service(
                this.current_robot_index,
                407,
                0,
                0);
            return result;
        }

        // Program reset with current robot index
        private int reset_program()
        {
            this.send_data = string.Empty;
            int result = this.request_service(
                 this.current_robot_index,
                 408,
                 0,
                 0);
            return result;
        }

        //Request service
        private int request_service(int robot_index, int request_ID, int send_data_length, int cycle_time)
        {
            this.current_robot_index = robot_index; //update current robot index
            if (
                   (request_ID < 100)
                || (request_ID > 427)
                || (request_ID == 109)
                || (request_ID >= 119 && request_ID <= 121)
                || (request_ID >= 134 && request_ID <= 136)
                || (request_ID >= 143 && request_ID <= 144)
                || (request_ID == 207)
                || (request_ID == 219)
                || (request_ID == 226)
                || (request_ID == 228)
                || (request_ID >= 243 && request_ID <= 254)
                || (request_ID >= 256 && request_ID <= 264)
                || (request_ID >= 311 && request_ID <= 314)
                || (request_ID >= 319 && request_ID <= 326)
                || (request_ID >= 328 && request_ID <= 342)
                || (request_ID >= 347 && request_ID <= 349)
                || (request_ID >= 351 && request_ID <= 362)
                || (request_ID >= 413 && request_ID <= 414)
                || (request_ID >= 422 && request_ID <= 425)
                )
            {
                return -100;
            }
            this.request_ID = request_ID.ToString(); //update property Request_ID
            if ((cycle_time < 0) || (cycle_time > 30000))
            {
                return -200;
            }
            this.cycle_time = cycle_time.ToString(); //update property Cycle_time
                                                     // Get Robot ID
            int robot_ID = robot_index;
            // Send Request
            int result = this.ax_.RequestServiceM
                (robot_ID,
                request_ID,
                send_data_length,
                this.send_data,
                this.display,
                int.Parse(this.cycle_time),
                0);
            return result;
        }

        //corresponding RequestService2NullM
        private int request(int robot_index, int request_ID, int cycle_time)//test with specified robot controllers
        {

            this.current_robot_index = robot_index; //update current robot index
            if (
                   (request_ID < 100)
                || (request_ID > 427)
                || (request_ID == 109)
                || (request_ID >= 119 && request_ID <= 121)
                || (request_ID >= 134 && request_ID <= 136)
                || (request_ID >= 143 && request_ID <= 144)
                || (request_ID == 207)
                || (request_ID == 219)
                || (request_ID == 226)
                || (request_ID == 228)
                || (request_ID >= 243 && request_ID <= 254)
                || (request_ID >= 256 && request_ID <= 264)
                || (request_ID >= 311 && request_ID <= 314)
                || (request_ID >= 319 && request_ID <= 326)
                || (request_ID >= 328 && request_ID <= 342)
                || (request_ID >= 347 && request_ID <= 349)
                || (request_ID >= 351 && request_ID <= 362)
                || (request_ID >= 413 && request_ID <= 414)
                || (request_ID >= 422 && request_ID <= 425)
                )
            {
                return -100;
            }
            this.request_ID = request_ID.ToString(); //update property Request_ID
            if ((cycle_time < 0) || (cycle_time > 30000))
            {
                return -200;
            }
            this.cycle_time = cycle_time.ToString(); //update property Cycle_time
                                                     // Get Robot ID
            int robot_ID = robot_index;
            //check if the middleware has disconnected with the request robot controller.
            if (!this.ax_.CheckConnectingM(robot_ID))
            {
                return -10;
            }
            int result = this.ax_.RequestService2NullM(
                                        robot_ID,
                                        request_ID,
                                        this.send_data,
                                        this.display,
                                        cycle_time,
                                        0
                                    );
            return result;
        }

        // Request Cancel
        private int request_cancel()
        {
            int robot_ID = int.Parse(Robot_list[current_robot_index].Split(':')[0]);
            int request_ID = int.Parse(this.request_ID);
            if (!this.ax_.CheckConnectingM(robot_ID))
            {
                return -10;
            }
            if (1 != this.ax_.RequestCancelM(
                        robot_ID,
                        request_ID
                    ))
            {
                return -20;
            }
            else
            {
                return 1;
            }
        }
    }

}
