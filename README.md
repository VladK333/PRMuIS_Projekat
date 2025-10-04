# 🌍 DERMS – Simulacija dispečerskog sistema za praćenje proizvodnje energije distribuiranih resursa  

## 📌 Opšti podaci
**Naziv projekta:** Simulacija dispečerskog sistema za praćenje proizvodnje energije distribuiranih energetskih resursa (DERMS)  
**Cilj:** Razvoj simulacionog sistema koji modeluje Smart Grid okruženje u kojem distribuirani generatori (solar, vetar) proizvode električnu energiju i komuniciraju sa centralnim dispečerskim serverom.  

---

## 📝 Sažetak
Sistem simulira rad **prosumer-a** (producer-consumer) i uključuje:  
- **Senzore** – generišu podatke o vremenskim prilikama (osunčanost, brzina vetra).  
- **DER generatore (klijente)** – na osnovu primljenih podataka računaju proizvodnju energije.  
- **Dispečerski server** – centralno prikuplja i obrađuje proizvodnju, šalje kontrolne poruke, reaguje na neočekivane vrednosti i upravlja radom DER-ova.  

---

## ⚙️ Tehnički opis
- **Komunikacija**  
  - **TCP** – između senzora i DER-a (slanje vremenskih prilika), između DER-a i dispečera (slanje izračunate proizvodnje).  
  - **UDP** – između dispečera i DER-ova (kontrolne poruke uključivanja/isključivanja).  

- **Simulacija vremenskih prilika**  
  - **Solarni panel:** generiše osunčanost (INS) i temperaturu ćelije (Tcell).  
  - **Vetrogenerator:** generiše nasumičnu brzinu vetra (0–30 m/s).  

- **Bezbednost**  
  - osnovna enkripcija komandi dispečera  

---
