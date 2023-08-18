


class api_base_exception(BaseException):
    description:str = ""

    def __init__(self, description:str=""):
        self.description = description


class connection_exception(api_base_exception):
    pass