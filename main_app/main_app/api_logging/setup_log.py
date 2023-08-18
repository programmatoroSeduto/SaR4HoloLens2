
from .logging import log




def setup_log(debug:bool = False, layer:int = 0, log_file_path:str = "", log_file_name:str = "") -> tuple[any, str]:
    success = True
    details = ""
    
    log_handle = log(
        debug=debug,
        layer=layer
    )
    if log_handle is None:
        return ( None, "ERROR: logger constructor returned None!")
    
    if log_file_path != "" and log_file_name != "":
        try:
            success = log_handle.set_log_file_main( log_file_path, log_file_name )
        except Exception as e:
            success = False
            details = f"Error during creation of the log file '{log_file_path}/{log_file_name}'\n\tReason: {e}"
    elif log_file_path == "":
        success = False
        details = "empty log file path"
    elif log_file_name == "":
        success = False
        details = "empty log file name"
    
    if success:
        return ( log_handle, details )
    else:
        return ( None, details )