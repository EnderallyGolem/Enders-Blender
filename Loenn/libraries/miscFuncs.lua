local miscFuncs = {}

-- ty chatgpt for generating this cause i have no idea how lua works
function miscFuncs.trimPath(path, defaultPath)
    if path == "" then
        path = defaultPath
    end

    while not path:match("^objects") do
        local slashIndex = path:find("/", 1, true)
        if not slashIndex then
            break -- Prevent infinite loop if '/' is not found
        end
        path = path:sub(slashIndex + 1)
    end

    local dotIndex = path:find("%.")
    if dotIndex then
        path = path:sub(1, dotIndex - 1)
    end

    return path
end

return miscFuncs