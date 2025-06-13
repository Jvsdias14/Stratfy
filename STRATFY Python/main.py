import subprocess
import threading
import time

def run_flask():
    subprocess.run(["python", "api_csv.py"])

def run_streamlit():
    subprocess.run(["streamlit", "run", "dashboard.py", "--server.port=8501"])

if __name__ == "__main__":
    threading.Thread(target=run_flask).start()
    time.sleep(2)  
    run_streamlit()