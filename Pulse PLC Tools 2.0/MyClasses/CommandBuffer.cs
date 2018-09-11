using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2._0
{
    public class CommandBufferItem
    {
        public Commands CommandCode { get; }
        public ILink Link { get; }
        public object CommandParams { get; }
        public int PauseAfterCmdMilliseconds { get; }

        public CommandBufferItem(Commands commandCode, ILink link, object commandParams, int pauseAfterCmdMilliseconds)
        {
            this.CommandCode = commandCode;
            this.Link = link;
            this.CommandParams = commandParams;
            this.PauseAfterCmdMilliseconds = pauseAfterCmdMilliseconds;
        }
    }

    public class CommandBuffer
    {
        public event EventHandler<StringMessageEventArgs> StringMessage = delegate { };
        public event EventHandler<EventArgs> CommandSended = delegate { };
        public event EventHandler<EventArgs> BufferCleared = delegate { };

        public int RepeatsAfterFail { get; set; }

        //Поток который мониторит наличие команд и отправляет их
        Thread handle_Thread;
        //Очередь команд
        private readonly Queue<CommandBufferItem> commands;
        //Максимальное количество команд которое было в очереди
        int cmd_counter_sum = 0;
        //Флаги выполнения команд
        bool busy_flag = false;
        bool Is_Command_Complete = true;   //Флаг статуса последней комманды (завершилась удачно или нет)
        bool haveCommandForCheck = false;
        int repeat_Counter;         //Счетчик повторных запросов

        public CommandBuffer(Protocol protocol)
        {
            RepeatsAfterFail = 3;
            //Обработчик события ответа на команду
            protocol.CommandAnswer += End_Command;
            commands = new Queue<CommandBufferItem>();
            //Запускаем поток
            handle_Thread = new Thread(Handle_CMD);
            handle_Thread.IsBackground = true;
            handle_Thread.Start(protocol);
        }

        public bool Buffer_Is_Emty()
        {
            return (commands.Count == 0);
        }

        public void Add_CMD(Commands cmd, ILink link, object param, int pause_After_ms)
        {
            //Добавляем команды
            commands.Enqueue(new CommandBufferItem(cmd, link, param, pause_After_ms));
            cmd_counter_sum++;

            if ((handle_Thread.ThreadState & ThreadState.Suspended) != 0) handle_Thread.Resume();
        }

        public void Clear_Buffer()
        {
            //Очищаем буффер
            commands.Clear();
            haveCommandForCheck = false;
            busy_flag = false;
            repeat_Counter = 0;
            cmd_counter_sum = 0;
            StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.ToolBarInfo, MessageString = "Отправка запросов завершена" });
            //Событие
            BufferCleared(this, new EventArgs());
        }

        //Поток отправляющий команды в фоне
        void Handle_CMD(object oProtocol)
        {
            while(true)
            {
                if (!busy_flag)
                {
                    //Проверяем статус предидущей комманды
                    if(haveCommandForCheck)
                    {
                        //Если команда выполнена успешно
                        if (Is_Command_Complete)
                        {
                            //Сообщение на форму
                            StringMessage(this, new StringMessageEventArgs() {
                                MessageType = Msg_Type.ToolBarInfo,
                                MessageString = "Запросы " + commands.Count + " из " + cmd_counter_sum + ". Нажми Esc для отмены.."
                            });

                            //Делаем заданную паузу после успешного выполнения
                            Thread.Sleep(commands.Peek().PauseAfterCmdMilliseconds);
                            //Двигаемся дальше ->
                            commands.Dequeue();
                            repeat_Counter = 0;     //Обнуляем ошибки если были
                            haveCommandForCheck = false;
                        }
                        else
                        {
                            if (repeat_Counter == RepeatsAfterFail) //Устройство не отвечает
                            {
                                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Устройство не отвечает" });
                                Clear_Buffer();
                            }
                            else
                            {
                                repeat_Counter++;
                                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Повторный запрос "+repeat_Counter+"..." });
                                if (((Protocol)oProtocol).Send_CMD(commands.Peek().CommandCode, commands.Peek().Link, commands.Peek().CommandParams))
                                {
                                    //Событие - Команда отправлена
                                    CommandSended(this, new EventArgs());
                                    //Флаги
                                    busy_flag = true;
                                    haveCommandForCheck = true;

                                }
                                else //Спорный момент !!! Доделать
                                {
                                    Clear_Buffer();
                                }
                            }
                        }
                    }
                    else
                    {
                        //Если в буффере есть комманды, то отправляем
                        if (commands.Count > 0)
                        {
                            if (((Protocol)oProtocol).Send_CMD(commands.Peek().CommandCode, commands.Peek().Link, commands.Peek().CommandParams))
                            {
                                //Сообщение на форму
                                StringMessage(this, new StringMessageEventArgs()
                                {
                                    MessageType = Msg_Type.ToolBarInfo,
                                    MessageString = "Запросы " + commands.Count + " из " + cmd_counter_sum + ". Нажми Esc для отмены.."
                                });
                                //Событие - Команда отправлена
                                CommandSended(this, new EventArgs());
                                //Флаги
                                busy_flag = true;
                                haveCommandForCheck = true;
                            }
                            else
                            {
                                Clear_Buffer();
                            }
                        }
                        else
                        {//Буффер пуст
                            cmd_counter_sum = 0;
                            StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.ToolBarInfo, MessageString = "Буфер команд пуст" });
                            //Событие
                            BufferCleared(this, new EventArgs());
                            handle_Thread.Suspend();
                        }
                    }
                }
                Thread.Sleep(30);
            }
            
        }

        public void End_Command(object sender, ProtocolEventArgs e)
        {
            Is_Command_Complete = e.IsHaveAnswer;
            busy_flag = false;
        }
    }

}
