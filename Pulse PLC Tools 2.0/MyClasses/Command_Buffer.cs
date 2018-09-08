using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2._0
{
    public class Command_Buffer
    {
        public event EventHandler<StringMessageEventArgs> StringMessage = delegate { };
        public event EventHandler<EventArgs> CommandSended = delegate { };
        public event EventHandler<EventArgs> BufferCleared = delegate { };

        Thread handle_Thread; 

        int BUF_SIZE = 1024; //Кратный степени двойки
        int BUF_MASK;       //= BUF_SIZE - 1;
        int REPEATS = 3;    //Количество повторных попыток
        //Указатели буфера
        int idx_IN = 0;
        int idx_OUT = 0;
        int cmd_counter = 0, cmd_counter_sum = 0;
        //Флаги выполнения команд
        bool busy_flag = false;
        bool Is_Command_Complete = true;   //Флаг статуса последней комманды (завершилась удачно или нет)
        bool haveCommandForCheck = false;
        int repeat_Counter;         //Счетчик повторных запросов

        //Буфферы данных для выполнения команд
        Commands[] buffer_commands;
        ILink[] buffer_links;
        object[] buffer_params;
        int[] buffer_pauseAfter_ms;

        public Command_Buffer(Protocol protocol)
        {
            //Обработчик события ответа на команду
            protocol.CommandAnswer += End_Command;


            //Переделать в очередь
            BUF_MASK = BUF_SIZE - 1;

            buffer_commands = new Commands[BUF_SIZE];
            buffer_links = new ILink[BUF_SIZE];
            buffer_params = new object[BUF_SIZE];
            buffer_pauseAfter_ms = new int[BUF_SIZE];

            handle_Thread = new Thread(Handle_CMD);
            handle_Thread.IsBackground = true;
            handle_Thread.Start(protocol);
        }

        public bool Buffer_Is_Emty()
        {
            return (idx_OUT == idx_IN);
        }

        public void Add_CMD(Commands cmd, ILink link, object param, int pause_After_ms)
        {
            //Добавляем команды
            buffer_commands[idx_IN] = cmd;
            buffer_links[idx_IN] = link;
            buffer_params[idx_IN] = param;
            buffer_pauseAfter_ms[idx_IN] = pause_After_ms;
            //Сдвигаем указатель
            idx_IN++;
            idx_IN &= BUF_MASK;
            cmd_counter++; //Считаем количество команд в буфере
            cmd_counter_sum++;

            if ((handle_Thread.ThreadState & ThreadState.Suspended) != 0) handle_Thread.Resume();
        }

        public void Clear_Buffer()
        {
            //Очищаем буффер
            idx_OUT = idx_IN;
            haveCommandForCheck = false;
            busy_flag = false;
            repeat_Counter = 0;
            cmd_counter_sum = 0;
            cmd_counter = 0;
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
                            cmd_counter--;//Считаем количество команд в буфере
                            //Сообщение на форму
                            StringMessage(this, new StringMessageEventArgs() {
                                MessageType = Msg_Type.ToolBarInfo,
                                MessageString = "Запросы " + cmd_counter + " из " + cmd_counter_sum + ". Нажми Esc для отмены.."
                            });

                            //Делаем заданную паузу после успешного выполнения
                            Thread.Sleep(buffer_pauseAfter_ms[idx_OUT]);
                            //Двигаемся дальше ->
                            idx_OUT++; //если сошлось то сдвигаем указатель дальше
                            idx_OUT &= BUF_MASK;    //зацикливаем если индекс больше чем BUF_SIZE
                            repeat_Counter = 0;     //Обнуляем ошибки если были
                            haveCommandForCheck = false;
                        }
                        else
                        {
                            if(buffer_commands[idx_OUT] == Commands.Check_Pass || buffer_commands[idx_OUT] == Commands.Search_Devices)
                            {
                                Clear_Buffer();
                            }
                            else
                            if (repeat_Counter == REPEATS) //Устройство не отвечает
                            {
                                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Устройство не отвечает" });
                                Clear_Buffer();
                            }
                            else
                            {
                                repeat_Counter++;
                                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Повторный запрос "+repeat_Counter+"..." });
                                if (((Protocol)oProtocol).Send_CMD(buffer_commands[idx_OUT], buffer_links[idx_OUT], buffer_params[idx_OUT]))
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
                        if (idx_IN != idx_OUT)
                        {
                            if (((Protocol)oProtocol).Send_CMD(buffer_commands[idx_OUT], buffer_links[idx_OUT], buffer_params[idx_OUT]))
                            {
                                //Сообщение на форму
                                StringMessage(this, new StringMessageEventArgs()
                                {
                                    MessageType = Msg_Type.ToolBarInfo,
                                    MessageString = "Запросы " + cmd_counter + " из " + cmd_counter_sum + ". Нажми Esc для отмены.."
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
