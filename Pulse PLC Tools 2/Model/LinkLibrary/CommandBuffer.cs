using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LinkLibrary
{
    public class CommandBufferItem
    {
        public IProtocol Protocol { get; }
        public int CommandCode { get; }
        public ILink Link { get; }
        public object CommandParams { get; }
        public int PauseAfterCmdMilliseconds { get; }

        public CommandBufferItem(ILink link, IProtocol protocol, int commandCode, object commandParams, int pauseAfterCmdMilliseconds)
        {
            this.Protocol = protocol;
            this.CommandCode = commandCode;
            this.Link = link;
            this.CommandParams = commandParams;
            this.PauseAfterCmdMilliseconds = pauseAfterCmdMilliseconds;
        }
    }

    public class CommandBuffer: IMessage
    {
        public event EventHandler<MessageDataEventArgs> Message = delegate { };
        public event EventHandler<EventArgs> CommandSended = delegate { };
        public event EventHandler<EventArgs> BufferCleared = delegate { };

        DispatcherTimer timerProtect; //Таймер для защиты от зависания, если в протоколе ошибка (не обработан CommandEnd)

        public int RepeatsAfterFail { get; set; }

        //Поток который мониторит наличие команд и отправляет их
        Thread handle_Thread;
        //Очередь команд
        private readonly Queue<CommandBufferItem> commands;
        //Максимальное количество команд которое было в очереди
        int countCommandsMax = 0;
        //Флаги выполнения команд
        bool busy_flag = false;
        bool Is_Command_Complete = true;   //Флаг статуса последней комманды (завершилась удачно или нет)
        bool haveCommandForCheck = false;
        int repeat_Counter;         //Счетчик повторных запросов

        public CommandBuffer()
        {
            RepeatsAfterFail = 3;
            commands = new Queue<CommandBufferItem>();
            //Запускаем поток
            handle_Thread = new Thread(Handle_CMD) { IsBackground = true };
            handle_Thread.Start();
        }

        public bool Buffer_Is_Emty()
        {
            return (commands.Count == 0);
        }

        public void Add_CMD(ILink link, IProtocol protocol, int commandCode,  object commandParams, int pauseAfterCmdMilliseconds)
        {
            //Добавляем команды
            commands.Enqueue(new CommandBufferItem(link, protocol, commandCode, commandParams, pauseAfterCmdMilliseconds));
            countCommandsMax++;

            if ((handle_Thread.ThreadState & ThreadState.Suspended) != 0) handle_Thread.Resume();
        }

        public void Clear_Buffer()
        {
            //Очищаем буффер
            commands.Clear();
            haveCommandForCheck = false;
            busy_flag = false;
            repeat_Counter = 0;
            countCommandsMax = 0;
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Отправка запросов завершена" });
            //Событие
            BufferCleared(this, new EventArgs());
        }

        //Поток отправляющий команды в фоне
        void Handle_CMD()
        {
            while(true)
            {
                if (!busy_flag)
                {
                    //Проверяем статус предидущей комманды
                    if(haveCommandForCheck)
                    {
                        commands.Peek().Link.DataRecieved -= commands.Peek().Protocol.DateRecieved;
                        commands.Peek().Protocol.CommandEnd -= CommandEndHandler;
                        //Если команда выполнена успешно
                        if (Is_Command_Complete)
                        {
                            //Сообщение на форму
                            Message(this, new MessageDataEventArgs() {
                                MessageType = MessageType.ToolBarInfo,
                                MessageString = "Запросы " + commands.Count + " из " + countCommandsMax + ". Нажми Esc для отмены.."
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
                                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Устройство не отвечает" });
                                Clear_Buffer();
                            }
                            else
                            {
                                repeat_Counter++;
                                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Повторный запрос " + repeat_Counter + "..." });
                                if (commands.Peek().Protocol.Send(commands.Peek().CommandCode, commands.Peek().Link, commands.Peek().CommandParams))
                                {
                                    commands.Peek().Link.DataRecieved += commands.Peek().Protocol.DateRecieved;
                                    commands.Peek().Protocol.CommandEnd += CommandEndHandler;
                                    //Событие - Команда отправлена
                                    CommandSended(this, new EventArgs());
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
                            if (commands.Peek().Protocol.Send(commands.Peek().CommandCode, commands.Peek().Link, commands.Peek().CommandParams))
                            {
                                commands.Peek().Link.DataRecieved += commands.Peek().Protocol.DateRecieved;
                                commands.Peek().Protocol.CommandEnd += CommandEndHandler;
                                //Событие - Команда отправлена
                                CommandSended(this, new EventArgs());
                                busy_flag = true;
                                haveCommandForCheck = true;
                                
                                //Сообщение на форму
                                Message(this, new MessageDataEventArgs()
                                {
                                    MessageType = MessageType.ToolBarInfo,
                                    MessageString = "Запросы " + commands.Count + " из " + countCommandsMax + ". Нажми Esc для отмены.."
                                });
                            }
                            else
                            {
                                Clear_Buffer();
                            }
                        }
                        else
                        {//Буффер пуст
                            countCommandsMax = 0;
                            Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Буфер команд пуст" });
                            //Событие
                            BufferCleared(this, new EventArgs());
                            handle_Thread.Suspend();
                        }
                    }
                }
                Thread.Sleep(30);
            }
            
        }

        public void CommandEndHandler(object sender, ProtocolEventArgs e)
        {
            Is_Command_Complete = e.Status;
            busy_flag = false;
        }
    }

}
