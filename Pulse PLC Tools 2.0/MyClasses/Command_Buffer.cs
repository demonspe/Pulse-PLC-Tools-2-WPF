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
        MainWindow main_Form;

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
        Command_type[] buffer_commands;
        Link[] buffer_links;
        object[] buffer_params;
        int[] buffer_pauseAfter_ms;

        public Command_Buffer(MainWindow mainForm_)
        {
            main_Form = mainForm_;

            BUF_MASK = BUF_SIZE - 1;

            buffer_commands = new Command_type[BUF_SIZE];
            buffer_links = new Link[BUF_SIZE];
            buffer_params = new object[BUF_SIZE];
            buffer_pauseAfter_ms = new int[BUF_SIZE];

            Thread handle_Thread = new Thread(Handle_CMD);
            handle_Thread.IsBackground = true;
            handle_Thread.Start(mainForm_.protocol);
        }

        public bool Buffer_Is_Emty()
        {
            return (idx_OUT == idx_IN);
        }

        public void Add_CMD(Command_type cmd, Link link, object param, int pause_After_ms)
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
        }

        public void End_Command(bool complete_Status)
        {
            Is_Command_Complete = complete_Status;
            busy_flag = false;
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
            main_Form.msg("Отправка запросов завершена");
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
                            main_Form.msg("Запросы " + cmd_counter + " из " + cmd_counter_sum +". Нажми Esc для отмены..");
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
                            if (repeat_Counter == REPEATS) //Устройство не отвечает
                            {
                                Clear_Buffer();
                                MessageBox.Show("Устройство не отвечает");
                            }
                            else
                            {
                                repeat_Counter++;
                                if(((Protocol)oProtocol).Send_CMD(buffer_commands[idx_OUT], buffer_links[idx_OUT], buffer_params[idx_OUT]))
                                {
                                    busy_flag = true;
                                    haveCommandForCheck = true;
                                }
                                else
                                {
                                    Clear_Buffer();
                                }
                                    
                            }
                        }
                    }
                    

                    //Если в буффере есть комманды, то отправляем
                    if (idx_IN != idx_OUT)
                    {
                        if (((Protocol)oProtocol).Send_CMD(buffer_commands[idx_OUT], buffer_links[idx_OUT], buffer_params[idx_OUT]))
                        {
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
                        main_Form.msg("Отправка запросов завершена");
                        Thread.Sleep(100);
                    }
                }
                Thread.Sleep(100);
            }
            
        }
    }
}
