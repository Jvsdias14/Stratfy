import streamlit as st
import pandas as pd
import requests
import matplotlib.pyplot as plt
import matplotlib.colors as mcolors
import plotly.express as px

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

# Função para gerar paleta de cores ajustada para contraste
def generate_contrasting_palette(base_hex_color, num_colors, reverse=False):
    base_rgb = mcolors.to_rgb(base_hex_color)
    palette = []
    h, s, l = mcolors.rgb_to_hsv(base_rgb)

    for i in range(num_colors):
        if num_colors == 1:
             current_l = max(0.1, l * 0.7)
        else:
            if reverse:
                current_l = max(0.1, l - (i / (num_colors - 1 + 0.01)) * (l * 0.6))
                current_s = min(1, s + (i / (num_colors - 1 + 0.01)) * (1 - s) * 0.5)
            else:
                current_l = min(0.9, l + (i / (num_colors - 1 + 0.01)) * (1 - l) * 0.6)
                current_s = max(0.2, s - (i / (num_colors - 1 + 0.01)) * s * 0.5)
        
        current_l = max(0.15, min(0.85, current_l))
        
        rgb_color = mcolors.hsv_to_rgb((h, current_l, current_s)) # Corrigido: h, l, s -> h, s, l. Mantendo h, s, l mesmo
        palette.append(mcolors.to_hex(rgb_color))

    return palette

if dashboard_id:
    url = f"http://localhost:5211/api/dashboarddata/{dashboard_id}"
    response = requests.get(url)

    if response.status_code == 200:
        data = response.json()
        df = pd.DataFrame(data["movimentacoes"])
        df.columns = df.columns.str.lower()
        st.title(f"Dashboard: {data['descricao']}")

        # ====== Filtros na barra lateral ======
        st.sidebar.header("Configurações")

        # Expander para Filtros de Dados
        with st.sidebar.expander("Filtros de Dados"):
            # Filtro por data
            if "datamovimentacao" in df.columns:
                # Converte para datetime logo no início para consistência
                df["datamovimentacao"] = pd.to_datetime(df["datamovimentacao"])
                datas = df["datamovimentacao"]
                data_inicio = st.date_input("Data inicial", datas.min())
                data_fim = st.date_input("Data final", datas.max())
                df_filtrado = df[(datas >= pd.to_datetime(data_inicio)) & (datas <= pd.to_datetime(data_fim))].copy()
            else:
                df_filtrado = df.copy()

            # Filtro por categoria
            if "categoria" in df.columns:
                categorias = df["categoria"].dropna().unique().tolist()
                categorias_selecionadas = st.multiselect("Categorias", categorias, default=categorias)
                df_filtrado = df_filtrado[df_filtrado["categoria"].isin(categorias_selecionadas)]
        
        # Expander para Opções de Visualização
        with st.sidebar.expander("Opções de Visualização"):
            st.subheader("Formato da Data")
            # Filtro de formato de data (movido para cá)
            if "datamovimentacao" in df.columns:
                formato_data_selecionado = st.radio(
                    "Selecione o formato da data para os gráficos",
                    ("Dia", "Dia e Mês", "Dia, Mês e Ano"),
                    index=2 # Padrão: Dia, Mês e Ano
                )

                formato_exibicao = "%Y-%m-%d" # Fallback
                if formato_data_selecionado == "Dia":
                    formato_exibicao = "%d"
                elif formato_data_selecionado == "Dia e Mês":
                    formato_exibicao = "%d-%m"
                elif formato_data_selecionado == "Dia, Mês e Ano":
                    formato_exibicao = "%d-%m-%Y"
                
                # Aplica o formato de exibição como uma nova coluna
                # IMPORTANTE: Converte para string (object) para que o Plotly não tente reformatar
                df_filtrado["data_exibicao"] = df_filtrado["datamovimentacao"].dt.strftime(formato_exibicao).astype(str)
            else:
                # Garante que 'data_exibicao' exista mesmo sem filtro de data
                df_filtrado["data_exibicao"] = df_filtrado["datamovimentacao"].astype(str) if "datamovimentacao" in df.columns else ""

            st.markdown("---") # Separador visual
            st.subheader("Cores do Texto")
            
            # Seletor para cor do texto dos Cartões
            text_color_cards = st.radio(
                "Cor do Texto nos Cartões",
                ("Preto", "Branco"),
                index=0 # Padrão: Preto
            )
            card_text_color = "black" if text_color_cards == "Preto" else "white"

            # Seletor para cor do texto dos Gráficos (Plotly e Matplotlib)
            text_color_charts = st.radio(
                "Cor do Texto nos Gráficos",
                ("Preto", "Branco"),
                index=1 # Padrão: Branco, para combinar com tema escuro que é comum
            )
            chart_text_color = "black" if text_color_charts == "Preto" else "white"


        st.markdown("---")

        # ====== Cartões (métricas) estilizados com CSS ======
        st.subheader("Cartões")

        if data["cartoes"]:
            colunas_cards = st.columns(len(data["cartoes"]))
            for i, card in enumerate(data["cartoes"]):
                campo = card["campo"].lower()
                tipo_agregacao = card["tipoAgregacao"].lower()
                cor_cartao = card.get("cor", "#f0f2f6")

                if campo in df.columns:
                    if tipo_agregacao == "soma":
                        valor = df_filtrado[campo].sum()
                        valor = valor.round(2)
                    elif tipo_agregacao == "media":
                        valor = df_filtrado[campo].mean().round(2)
                    elif tipo_agregacao == "max":
                        valor = df_filtrado[campo].max()
                    elif tipo_agregacao == "min":
                        valor = df_filtrado[campo].min()
                    elif tipo_agregacao == "contagem":
                        valor = df_filtrado[campo].nunique()
                    else:
                        valor = "Agregação inválida"

                    cor_texto_card = card_text_color 

                    card_html = f"""
                        <div style="background-color: {cor_cartao};
                                     padding: 15px;
                                     border-radius: 5px;
                                     box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
                                     text-align: center;">
                            <div style="color: {cor_texto_card}; font-size: 1.5em; font-weight: bold; margin-top: 0; margin-bottom: 5px;">{card["nome"]}</div>
                            <p style="color: {cor_texto_card}; font-size: 1.5em; margin-bottom: 0;">{valor}</p>
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

                        is_campo2_numeric = pd.api.types.is_numeric_dtype(df_filtrado[campo2])
                        
                        # Verifica se campo1 é uma coluna de data no DataFrame ORIGINAL
                        is_campo1_original_date = pd.api.types.is_datetime64_any_dtype(df[campo1])

                        # Lógica para gráficos de Barras e Linhas
                        if tipo in ["barra", "linha"]:
                            # Se o campo1 original é uma data, usa 'data_exibicao' e renomeia o rótulo
                            if is_campo1_original_date and is_campo2_numeric:
                                # Agrupa e garante que o índice resultante seja tratado como categórico para o Plotly
                                # Ordenar antes de agrupar para manter a ordem cronológica
                                grouped_df_for_plot = df_filtrado.sort_values(by="datamovimentacao").groupby("data_exibicao")[campo2].sum().reset_index()
                                
                                if tipo == "barra":
                                    fig = px.bar(grouped_df_for_plot, x="data_exibicao", y=campo2, 
                                                 title=graf["titulo"], color_discrete_sequence=[cor],
                                                 labels={"data_exibicao": "Data", campo2: campo2.capitalize()}) # Renomeia o eixo X
                                else:
                                    fig = px.line(grouped_df_for_plot, x="data_exibicao", y=campo2, 
                                                  title=graf["titulo"], color_discrete_sequence=[cor],
                                                  labels={"data_exibicao": "Data", campo2: campo2.capitalize()}) # Renomeia o eixo X
                                
                                # Força o eixo X a ser tratado como categoria para não reformatar a data string
                                fig.update_xaxes(type='category')

                                # Aplicar cor do texto para Plotly
                                fig.update_layout(
                                    font_color=chart_text_color,
                                    xaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    yaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    title_font=dict(color=chart_text_color)
                                )
                                st.plotly_chart(fig, use_container_width=True)

                            elif not is_campo1_original_date and is_campo2_numeric: # Campo1 é categórico, Campo2 é numérico
                                agrupado = df_filtrado.groupby(campo1)[campo2].sum().reset_index() # reset_index para ter colunas nomeadas
                                fig = px.bar(agrupado, x=campo1, y=campo2, # Usa nomes das colunas após reset_index
                                             title=graf["titulo"], color_discrete_sequence=[cor],
                                             labels={campo1: campo1.capitalize(), campo2: campo2.capitalize()})
                                
                                # Aplicar cor do texto para Plotly
                                fig.update_layout(
                                    font_color=chart_text_color,
                                    xaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    yaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    title_font=dict(color=chart_text_color)
                                )
                                st.plotly_chart(fig, use_container_width=True)

                            # Caso: Eixo X e Y são categóricos (barras empilhadas com Plotly)
                            elif not is_campo1_original_date and not is_campo2_numeric:
                                df_grouped = df_filtrado.groupby([campo1, campo2]).size().reset_index(name='count')

                                unique_types_in_stack = df_grouped[campo2].unique()
                                plotly_palette = generate_contrasting_palette(cor, len(unique_types_in_stack), reverse=True)

                                fig = px.bar(
                                    df_grouped,
                                    x=campo1,
                                    y='count',
                                    color=campo2,
                                    title=graf["titulo"],
                                    labels={'count': 'Contagem', campo1: campo1.capitalize(), campo2: campo2.capitalize()},
                                    color_discrete_sequence=plotly_palette
                                    
                                )
                                fig.update_xaxes(tickangle=45)
                                # Aplicar cor do texto para Plotly
                                fig.update_layout(
                                    font_color=chart_text_color,
                                    xaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    yaxis=dict(tickfont=dict(color=chart_text_color), title_font=dict(color=chart_text_color)),
                                    legend=dict(font=dict(color=chart_text_color)), # Cor da legenda
                                    title_font=dict(color=chart_text_color)
                                )
                                st.plotly_chart(fig, use_container_width=True)
                            else:
                                st.warning(f"Combinação de campos ou tipo de gráfico inválida para {graf['titulo']}.")

                        # Lógica para gráficos de Pizza
                        elif tipo == "pizza":
                            if is_campo2_numeric:
                                dados = df_filtrado.groupby(campo1)[campo2].sum().abs()
                            else:
                                dados = df_filtrado[campo1].value_counts().abs()

                            n = len(dados)
                            pizza_palette = generate_contrasting_palette(cor, n)

                            fig, ax = plt.subplots()
                            
                            fig.patch.set_alpha(0.0) 
                            ax.set_facecolor((0, 0, 0, 0)) 

                            wedges, texts, autotexts = ax.pie(
                                dados,
                                labels=dados.index,
                                autopct='%1.1f%%',
                                startangle=90,
                                colors=pizza_palette,
                                pctdistance=0.85,
                                labeldistance=1.15
                            )
                            ax.axis('equal')

                            # Usa a cor de texto selecionada pelo usuário para gráficos Matplotlib
                            for text in texts + autotexts:
                                text.set_color(chart_text_color)
                            
                            st.pyplot(fig)
                        else:
                            st.warning(f"Tipo de gráfico '{tipo}' ou campos inválidos para o gráfico.")
                    else:
                        st.warning(f"Campos inválidos no gráfico: {campo1}, {campo2}")

    else:
        st.error("Dashboard não encontrada.")
else:
    st.warning("Nenhum ID de dashboard foi informado na URL.")