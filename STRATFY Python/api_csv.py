from flask import Flask, request, jsonify
import pandas as pd
from datetime import datetime
from flask_cors import CORS

app = Flask(__name__)
CORS(app)  # permite requisições do ASP.NET no front

# Dicionário de mapeamento por banco
mapeamentos = {
    "Banco do Brasil": {
        "descricao": "Histórico",
        "valor": "Valor",
        "tipo": "Tipo",
        "data": "Data",
    },
    "Caixa": {
        "descricao": "Descrição",
        "valor": "Valor final",
        "tipo": "Forma Pagamento",
        "data": "Data Operação",
    },
    "Itaú": {
        "descricao": "Detalhes",
        "valor": "Valor (R$)",
        "tipo": "Tipo Operação",
        "data": "Data",
    },
    "Nubank": {
        "descricao": "description",
        "valor": "amount",
        "tipo": "transaction_type",
        "data": "time",
    },
    "Bradesco": {
        "descricao": "Histórico",
        "valor": "Valor Lançamento",
        "tipo": "Tipo de Lançamento",
        "data": "Data Lançamento",
    }
}

@app.route('/api/uploadcsv', methods=['POST'])
def processar_csv():
    if 'file' not in request.files or 'banco' not in request.form:
        return jsonify({'erro': 'Arquivo ou banco ausente'}), 400

    file = request.files['file']
    banco = request.form['banco']

    if file.filename == '':
        return jsonify({'erro': 'Arquivo vazio'}), 400

    if banco not in mapeamentos:
        return jsonify({'erro': f'Banco não suportado: {banco}'}), 400

    try:
        # Tenta leitura em utf-8
        try:
            df = pd.read_csv(file, sep=None, engine='python', encoding='utf-8')
        except UnicodeDecodeError:
            file.stream.seek(0)  # volta ao início do stream
            df = pd.read_csv(file, sep=None, engine='python', encoding='latin1')  # fallback

        mapeamento = mapeamentos[banco]

        df.columns = df.columns.str.strip()  # remove espaços
        df = df.rename(columns={v: k for k, v in mapeamento.items() if v in df.columns})

        # Verifica se todos os campos necessários foram mapeados
        campos_necessarios = ["descricao", "valor", "tipo", "data"]
        if not all(c in df.columns for c in campos_necessarios):
            return jsonify({'erro': f'Colunas obrigatórias ausentes: {df.columns.tolist()}'}), 400

        # Formatação de data (tratamento flexível)
        try:
            df["data"] = pd.to_datetime(df["data"]).dt.date
        except:
            df["data"] = pd.to_datetime(df["data"], errors='coerce').dt.date

        # Remover NaNs e formatar saída
        df = df.dropna(subset=["descricao", "valor", "tipo", "data"])

        movimentacoes = []
        for _, row in df.iterrows():
            movimentacoes.append({
                "Descricao": str(row["descricao"]),
                "Valor": float(row["valor"]),
                "Tipo": str(row["tipo"]),
                "DataMovimentacao": row["data"].isoformat()
            })

        return jsonify(movimentacoes)

    except Exception as e:
        return jsonify({'erro': str(e)}), 500


if __name__ == "__main__":
    app.run(port=8000, debug=True, use_reloader=False)