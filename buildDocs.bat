git clone https://github.com/cetusfinance/qwackdocs.git origin_site -q
wyam build -o docs
CD docs
git add -A 2>&1
git commit -m "CI Updates" -q
git push origin -q
