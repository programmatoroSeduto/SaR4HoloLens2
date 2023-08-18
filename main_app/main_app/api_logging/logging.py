
from main_app import exceptions
from typing import Union
from datetime import datetime
from threading import Lock as lock

from .log_type import log_type
from .log_layer import log_layer







class log:
    '''main logging class
    
    this class is meant to manage the logging in a handy and efficient way. 

    Features:
        Log Layer:
            a integer enabling to filter log messages. Set as biggest
            as possible if unused. 
    '''
    
    def __init__(self, layer:int = 0, debug:str = False):
        self.__log_debug = debug
        self.__current_log_layer = layer
        self.__use_log_layer = ( layer >= 0 )
        
        self.__log_history = list()
        self.__log_timestamp_format = "%d-%m-%Y %H:%M:%S"
        
        self.__log_file_timestamp_format = "%d%m%Y_%H%M%S"
        self.__use_log_file_main = False
        self.__log_main_path = ""
        self.__log_main_fil = None
        self.__log_file_lock = None
    

    def set_log_file_main(self, path:str = "", file_name:str = "") -> bool:
        if path == "" or file_name == "":
            return False
        if self.__use_log_file_main:
            return True
        
        tmstp = self.__get_file_timestamp()
        self.__log_main_path = f"{path}/{file_name}_{tmstp}.txt"
        self.__log_main_fil = open( self.__log_main_path, 'w' ) # can raise exc!
        self.__log_file_lock = lock()
        self.__use_log_file_main = True
        
        return True
    

    def set_layer(self, use_layer:bool = True, layer:int = 0) -> None:
        if use_layer:
            self.__use_log_layer = True
            self.__current_log_layer = layer
        else:
            self.__use_log_layer = False
            self.__current_log_layer = -1
    

    def get_layer(self) -> Union[int, None]:
        if self.__use_log_layer:
            return self.__current_log_layer
        else:
            return None


    def debug_detail(self, msg:str = "", layer:int = log_layer.working_step_detail, src:str = "???") -> None:
        if self.__log_debug:
            return
        ss = f"{self.__get_log_header(log_type.debug, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.debug, src)


    def debug(self, msg:str = "", layer:int = log_layer.working_phase, src:str = "???") -> None:
        if self.__log_debug:
            return
        ss = f"{self.__get_log_header(log_type.debug, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.debug, src)


    def info(self, msg:str = "", layer:int = log_layer.working_step, src:str = "???") -> None:
        ss = f"{self.__get_log_header(log_type.info, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.info, src)


    def info_api(self, path:str, layer:int = log_layer.api_access, src:str = "???") -> None:
        msg = f"API ACCESS: {path}"
        ss = f"{self.__get_log_header(log_type.info, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.info, "\n", src)


    def warn(self, msg:str = "", layer:int = log_layer.important, src:str = "???") -> None:
        ss = f"{self.__get_log_header(log_type.warning, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.warming, src)


    def err(self, msg:str = "", layer:int = log_layer.critical, raise_exc:bool = False, src:str = "???") -> None:
        ss = f"{self.__get_log_header(log_type.error, layer, src)} {msg}"
        self.__log_print(ss, layer, log_type.error, src)
        if raise_exc:
            raise exceptions.api_base_exception(
                description=ss
            )
    

    def get_history(self, tail_size:int = -1) -> list[str]:
        if tail_size > 0:
            log_len = len(self.__log_history)
            if( log_len <= tail_size ):
                return self.__log_history
            else:
                return self.__log_history[:-log_len]
        else:
            return self.__log_history
    

    def __log_print(self, ss, layer:int, ltype:log_type, src=str) -> None:
        self.__log_history.append(self.__log_history_item(ss, layer, ltype, src))
        if(int(layer) > int(self.__current_log_layer)):
            return
        print(ss)
        if self.__use_log_file_main:
            if self.__log_file_lock is not None:
                with self.__log_file_lock:
                    self.__log_main_fil.write(ss + ( "\n" if not ss.endswith("\n") else ""))
                    self.__log_main_fil.flush()
            else:
                print("(log file mutex was None) LOST LOG: " + ss)
    

    def __log_history_item(self, ss:str, layer:int, type:log_type, src:str) -> dict:
        return {
            'timestamp' : datetime.now().strftime(self.__log_timestamp_format),
            'log_source_component' : src,
            'log_layer' : str(layer),
            'log_type' : str(layer),
            'log_content' : ss
        }


    def __get_timestamp(self) -> str:
        return datetime.now().strftime(self.__log_timestamp_format)


    def __get_file_timestamp(self) -> str:
        return datetime.now().strftime(self.__log_file_timestamp_format)


    def __get_log_header(self, type:log_type, layer:int, src:str) -> str:
        return f"[{self.__get_timestamp()}, {type}, {layer}, '{src}']"