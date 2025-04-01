import pandas as pd
import json
import xml.etree.ElementTree as ET
#import pdfplumber
import csv
from datetime import datetime
#from sqlalchemy import create_engine
from flask import Flask, request, jsonify
import pandas as pd
import io
import matplotlib.pyplot as plt

'''
app = Flask(__name__)

@app.route("/api/upload", methods=["POST"])
def upload_file():
    if "file" not in request.files:
        return jsonify({"error": "Nenhum arquivo enviado"}), 400

    file = request.files["file"]
    
    # Lê o arquivo diretamente da memória sem salvá-lo no disco
    df = pd.read_csv(io.StringIO(file.stream.read().decode("utf-8")))

    print(df.head())  # Apenas para debug

    return jsonify({
        "message": "Arquivo processado!",
        "columns": list(df.columns)
    }), 200

if __name__ == "__main__":
    app.run(debug=True, port=5000)'''

# CONFIGURAÇÃO DO BANCO DE DADOS SQL SERVER
'''server = "SEU_SERVIDOR"
database = "SEU_BANCO"
username = "SEU_USUARIO"
password = "SUA_SENHA"
conn_str = f"mssql+pyodbc://{username}:{password}@{server}/{database}?driver=ODBC+Driver+17+for+SQL+Server"
engine = create_engine(conn_str)'''

def parse_date(date_str):
    """Converte datas para o formato YYYY-MM-DD"""
    for fmt in ("%d/%m/%Y", "%Y-%m-%d", "%m-%d-%Y", "%d-%m-%Y"):
        try:
            return datetime.strptime(date_str, fmt).strftime("%Y-%m-%d")
        except ValueError:
            continue
    return None

'''
def read_json(file_path):
    with open(file_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    transactions = [
        {"data": parse_date(item.get("date")), "descrição": item.get("description"), "valor": float(item.get("amount", 0))}
        for item in data.get("transactions", [])
    ]
    return pd.DataFrame(transactions)'''

'''
def read_xml(file_path):
    tree = ET.parse(file_path)
    root = tree.getroot()
    transactions = [
        {"data": parse_date(item.findtext("date")), "descrição": item.findtext("description"), "valor": float(item.findtext("amount", 0))}
        for item in root.findall(".//transaction")
    ]
    return pd.DataFrame(transactions)'''

'''
def read_csv_xlsx(file_path):
    df = pd.read_csv(file_path) if file_path.endswith(".csv") else pd.read_excel(file_path)
    df.rename(columns={"Date": "data", "Description": "descrição", "Amount": "valor"}, inplace=True)
    df["data"] = df["data"].apply(parse_date)
    df["valor"] = df["valor"].astype(float)

                      
    return df'''


'''def read_pdf(file_path):
    transactions = []
    with pdfplumber.open(file_path) as pdf:
        for page in pdf.pages:
            text = page.extract_text()
            if text:
                for line in text.split("\n"):
                    parts = line.split()
                    if len(parts) >= 3:
                        date, description, value = parts[0], " ".join(parts[1:-1]), parts[-1]
                        transactions.append({"data": parse_date(date), "descrição": description, "valor": float(value.replace(",", "."))})
    return pd.DataFrame(transactions)'''
'''
def process_file(file_path):
    """Identifica o tipo do arquivo e lê os dados"""
    if file_path.endswith(".json"):
        return read_json(file_path)
    elif file_path.endswith(".xml"):
        return read_xml(file_path)
    elif file_path.endswith(".csv") or file_path.endswith(".xlsx"):
        return read_csv_xlsx(file_path)
    elif file_path.endswith(".pdf"):
        return read_pdf(file_path)
    else:
        raise ValueError("Formato de arquivo não suportado")'''

'''
def save_to_sql(df, table_name):
    """Salva os dados no SQL Server"""
    df.to_sql(table_name, con=engine, if_exists="append", index=False)
    print(f"Dados salvos na tabela {table_name} com sucesso!")'''

'''
def load_from_sql(table_name):
    """Carrega os dados do SQL Server e retorna um DataFrame"""
    df = pd.read_sql(f"SELECT * FROM {table_name}", con=engine)
    df["data"] = pd.to_datetime(df["data"])
    df["valor"] = df["valor"].astype(float)
    return df'''



'''# EXEMPLO DE USO
if __name__ == "__main__":
    file_path = "extrato.json"  # Substitua pelo caminho do arquivo real
    try:
        df = process_file(file_path)
        print(df.head())
        save_to_sql(df, "extratos")
        df_loaded = load_from_sql("extratos")
        generate_plot(df_loaded)
    except Exception as e:
        print(f"Erro: {e}")'''


file_path = "Exemplo Extrato.csv"
df = pd.read_csv(file_path) if file_path.endswith(".csv") else pd.read_excel(file_path)
df.columns = df.columns.str.strip()
df.rename(columns={df.columns[0]: "Data", df.columns[1]: "Descrição", df.columns[2]: "Valor"}, inplace=True)
df["Data"] = df["Data"].apply(parse_date)
df["Valor"] = df["Valor"].astype(float)

df["Data"] = pd.to_datetime(df["Data"])  # Garantir que a coluna "Data" esteja no formato datetime
df_positive = df[df["Valor"] > 0].groupby("Data")["Valor"].sum()
df_negative = df[df["Valor"] < 0].groupby("Data")["Valor"].sum()

# Criar os gráficos
fig, axes = plt.subplots(2, 1, figsize=(10, 8), sharex=False)

# Gráfico de valores positivos
df_positive.plot(kind="bar", ax=axes[0], color="green", title="Soma dos Valores Positivos por Data")
axes[0].set_ylabel("Valor (Positivo)")

# Gráfico de valores negativos
df_negative.plot(kind="bar", ax=axes[1], color="red", title="Soma dos Valores Negativos por Data")
axes[1].set_ylabel("Valor (Negativo)")

# Ajustar layout
plt.xlabel("Data")
plt.tight_layout()
plt.show()

'''grafico1 = [df["Valor"]>0].plot(title="Gastos por data", xlabel="Data", ylabel="Valor", kind="bar")

#df.groupby("Data")["Valor"].sum().plot(title="Gastos por data", xlabel="Data", ylabel="Valor")

print(grafico1)
               
print(df)
df.describe()'''
