import streamlit as st
import pandas as pd
import requests
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors

st.set_page_config(layout="wide")

# Captura parâmetros da URL
query = st.query_params
dashboard_id = str(query.get("dashboardId", "")).strip()

def get_luminosity(hex_color):
    """Calcula a luminosidade de uma cor hexadecimal."""
    hex_color = hex_color.lstrip('#')
    r = int(hex_color[0:2], 16) / 255
    g = int(hex_color[2:4], 16) / 255
    b = int(hex_color[4:6], 16) / 255
    return (0.299 * r + 0.587 * g + 0.114 * b)

if dashboard_id:
    url = f"http://localhost:5211/api/dashboarddata/{dashboard_id}"
    response = requests.get(url)

    if response.status_code == 200:
        data = response.json()
        df = pd.DataFrame(data["movimentacoes"])
        df.columns = df.columns.str.lower()
        st.title(f"Dashboard: {data['dashboard']['descricao']}")

        # ====== Filtros na barra lateral ======
        st.sidebar.header("Filtros")

        # Filtro por data
        if "datamovimentacao" in df.columns:
            datas = pd.to_datetime(df["datamovimentacao"])
            data_inicio = st.sidebar.date_input("Data inicial", datas.min())
            data_fim = st.sidebar.date_input("Data final", datas.max())
            df_filtrado = df[(datas >= pd.to_datetime(data_inicio)) & (datas <= pd.to_datetime(data_fim))].copy() # Use .copy() para evitar SettingWithCopyWarning

            # Filtro de formato de data
            formato_data_selecionado = st.sidebar.radio(
                "Formato da Data",
                ("Dia", "Dia e Mês", "Dia, Mês e Ano"),
                index=2 # Define "Dia, Mês e Ano" como padrão
            )

            formato_exibicao = "%Y-%m-%d"  # Formato padrão (ano-mês-dia)
            if formato_data_selecionado == "Dia":
                formato_exibicao = "%d"
            elif formato_data_selecionado == "Dia e Mês":
                formato_exibicao = "%d-%m"
            elif formato_data_selecionado == "Dia, Mês e Ano":
                formato_exibicao = "%d-%m-%Y"

            # Formata a coluna de data para exibição
            df_filtrado["data_exibicao"] = pd.to_datetime(df_filtrado["datamovimentacao"]).dt.strftime(formato_exibicao)
        else:
            df_filtrado = df.copy() # Se não houver coluna de data, use o DataFrame original

        # Filtro por categoria
        if "categoria" in df.columns:
            categorias = df["categoria"].dropna().unique().tolist()
            categorias_selecionadas = st.sidebar.multiselect("Categorias", categorias, default=categorias)
            df_filtrado = df_filtrado[df_filtrado["categoria"].isin(categorias_selecionadas)]

        st.markdown("---")

        # ====== Cartões (métricas) estilizados com CSS ======
        st.subheader("Métricas")
        if data["cartoes"]:
            colunas_cards = st.columns(len(data["cartoes"]))
            for i, card in enumerate(data["cartoes"]):
                campo = card["campo"].lower()
                tipo_agregacao = card["tipoAgregacao"].lower()
                cor_cartao = card.get("cor", "#f0f2f6") # Obtém a cor do cartão da API

                if campo in df.columns:
                    if tipo_agregacao == "soma":
                        valor = df_filtrado[campo].sum() # Use df_filtrado
                    elif tipo_agregacao == "media":
                        valor = df_filtrado[campo].mean() # Use df_filtrado
                    elif tipo_agregacao == "max":
                        valor = df_filtrado[campo].max() # Use df_filtrado
                    elif tipo_agregacao == "min":
                        valor = df_filtrado[campo].min() # Use df_filtrado
                    elif tipo_agregacao == "contagem":
                        valor = df_filtrado[campo].nunique() # Use df_filtrado
                    else:
                        valor = "Agregação inválida"

                    # Determina a cor do texto para contraste
                    luminosidade = get_luminosity(cor_cartao)
                    cor_texto = "black" if luminosidade > 0.5 else "white"

                    # Cria o HTML para o cartão com estilos inline
                    card_html = f"""
                        <div style="background-color: {cor_cartao};
                                    padding: 15px;
                                    border-radius: 5px;
                                    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                                    text-align: center;">
                            <h3 style="color: {cor_texto}; margin-top: 0; margin-bottom: 5px;">{card["nome"]}</h3>
                            <p style="color: {cor_texto}; font-size: 1.5em; margin-bottom: 0;">{valor}</p>
                        </div>
                    """
                    colunas_cards[i].markdown(card_html, unsafe_allow_html=True)
                else:
                    colunas_cards[i].warning(f"Campo inválido: {campo}")

        st.markdown("---")

        # ====== Gráficos ======
        st.subheader("Gráficos")
        graficos = data["graficos"]
        num_por_linha = 2

        for i in range(0, len(graficos), num_por_linha):
            colunas = st.columns(num_por_linha)
            for j, graf in enumerate(graficos[i:i + num_por_linha]):
                with colunas[j]:
                    campo1 = graf["campo1"].lower()
                    campo2 = graf["campo2"].lower()
                    tipo = graf["tipo"].lower()
                    cor = graf.get("cor", "blue").lower()

                    if campo1 in df_filtrado.columns and campo2 in df_filtrado.columns:
                        st.subheader(graf["titulo"])
                        if tipo == "barra" and campo1 == "datamovimentacao" and "data_exibicao" in df_filtrado.columns:
                            agrupado = df_filtrado.groupby("data_exibicao")[campo2].sum()
                            st.bar_chart(agrupado, color=cor)
                        elif tipo == "linha" and campo1 == "datamovimentacao" and "data_exibicao" in df_filtrado.columns:
                            agrupado = df_filtrado.groupby("data_exibicao")[campo2].sum()
                            st.line_chart(agrupado, color=cor)
                        elif tipo == "pizza":
                            dados = df_filtrado.groupby(campo1)[campo2].sum().abs()

                            # Gerar uma paleta de tons da cor base
                            base_color = mcolors.to_rgb(cor)
                            n = len(dados)
                            palette = [mcolors.to_hex((min(1, base_color[0] + i/n),
                                                        min(1, base_color[1] + i/n),
                                                        min(1, base_color[2] + i/n))) for i in range(n)]

                            fig, ax = plt.subplots()
                            ax.pie(dados, labels=dados.index, autopct='%1.1f%%', startangle=90, colors=palette)
                            ax.axis('equal')
                            st.pyplot(fig)
                        elif tipo in ["barra", "linha"] and campo1 != "datamovimentacao":
                            agrupado = df_filtrado.groupby(campo1)[campo2].sum()
                            if tipo == "barra":
                                st.bar_chart(agrupado, color=cor)
                            else:
                                st.line_chart(agrupado, color=cor)
                        else:
                            st.warning(f"Tipo de gráfico '{tipo}' ou campos inválidos para o gráfico.")
                    else:
                        st.warning(f"Campos inválidos no gráfico: {campo1}, {campo2}")

    else:
        st.error("Dashboard não encontrada.")
else:
    st.warning("Nenhum ID de dashboard foi informado na URL.")